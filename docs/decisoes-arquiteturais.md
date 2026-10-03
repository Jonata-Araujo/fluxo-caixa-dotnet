# Registro de Decisões Arquiteturais (ADRs)

Formato curto: **contexto → decisão → alternativas consideradas → consequências**.

---

## ADR-001: Separar Lançamentos e Consolidado em dois serviços comunicando por mensageria

**Contexto.** Requisito não funcional: *"o serviço de controle de lançamentos não deve ficar indisponível se o sistema de consolidado diário cair"*. Além disso, as duas cargas têm perfis diferentes: escrita transacional de um lado, leitura intensa (picos de 50 req/s) do outro.

**Decisão.** Dois serviços implantáveis de forma independente, cada um com seu banco lógico. A integração é **exclusivamente assíncrona** via RabbitMQ, com o evento `transaction.registered`.

**Alternativas.**
- *Monólito com relatório calculado on-the-fly (`SUM ... GROUP BY`)*: mais simples, mas a carga de relatório competiria com o registro de lançamentos, e uma falha derrubaria os dois.
- *Chamada HTTP síncrona Lançamentos → Consolidado*: acoplamento temporal, que viola diretamente o NFR.

**Consequências.** (+) Isolamento de falhas e escalabilidade independente. (+) Leitura O(1) por dia no consolidado. (−) **Consistência eventual** (atraso típico de ~1s), aceitável para fluxo de caixa. (−) Mais peças para operar (broker), mitigado com Docker Compose e health checks.

---

## ADR-002: Transactional Outbox para publicar eventos

**Contexto.** Salvar o lançamento e publicar no broker são duas operações em recursos diferentes (*dual write*). Se o broker estiver fora, ou o processo cair entre as duas, perde-se o evento ou o lançamento.

**Decisão.** O lançamento e a mensagem (`outbox_messages`) são gravados **na mesma transação** do PostgreSQL. Um `BackgroundService` lê mensagens pendentes com `FOR UPDATE SKIP LOCKED`, publica com **publisher confirms** e mensagens persistentes, e só então marca como processada. A publicação usa `mandatory: true`: se ainda não existir fila ligada ao exchange (ex.: o Consolidado nunca subiu), o broker devolve a mensagem e ela continua pendente, sem descarte silencioso.

**Alternativas.** Publicar direto após o `SaveChanges` (perde eventos); transação distribuída/2PC (complexa e não suportada pelo RabbitMQ); CDC com Debezium (robusto, porém pesado para o escopo).

**Consequências.** (+) O `POST` não depende do broker. (+) Entrega *at-least-once*. (+) `SKIP LOCKED` permite várias instâncias do serviço drenando o outbox em paralelo. (−) Possíveis duplicatas, tratadas pela ADR-003. (−) Tabela de outbox cresce, mitigado com índice parcial (`WHERE processed_at IS NULL`); limpeza periódica fica como melhoria.

---

## ADR-003: Consumidor idempotente com upsert incremental atômico

**Contexto.** Com entrega *at-least-once*, o mesmo evento pode chegar mais de uma vez. Vários consumidores (ou um consumidor com prefetch) podem atualizar o **mesmo dia** simultaneamente.

**Decisão.** Em uma única transação:
1. `INSERT INTO processed_transactions (transaction_id) ... ON CONFLICT DO NOTHING`. Se nenhuma linha foi inserida, o evento é duplicado e é ignorado.
2. `INSERT INTO daily_balances ... ON CONFLICT (balance_date) DO UPDATE SET total_credits = daily_balances.total_credits + EXCLUDED.total_credits, ...`

**Alternativas.**
- *Ler → somar na aplicação → salvar* com concorrência otimista (`xmin`/rowversion): funciona, mas gera conflitos e retries justamente no pico (todos os lançamentos do dia disputam a mesma linha).
- *Lock pessimista (`SELECT ... FOR UPDATE`)*: correto, porém com mais round-trips.
- *Recalcular o dia inteiro a cada evento*: custo cresce com o volume do dia.

**Consequências.** (+) Sem *lost update*, sem retries de concorrência, um round-trip por instrução. (+) Reprocessar a fila inteira é seguro. (−) A regra "saldo += valor" existe no domínio (`DailyBalance.Apply`, usada nos testes e na semântica) e também em SQL. Esse trade-off é consciente e coberto por testes de integração de concorrência.

---

## ADR-004: Quorum queue, delivery limit e DLQ

**Contexto.** É preciso distinguir **falha transitória** (banco indisponível), que deve ser tentada de novo sem perder a mensagem, de **mensagem inválida** (*poison message*), que nunca vai ser processável e não pode travar a fila.

**Decisão.**
- Ack manual e `prefetch = 50` (backpressure).
- Falha transitória: até 3 tentativas em memória com backoff exponencial e depois `nack(requeue: true)`.
- Fila **quorum** com `x-delivery-limit = 10`: ao exceder, o próprio RabbitMQ envia para a DLQ (`consolidation.transaction-registered.dlq`).
- Payload inválido ou violação de regra: `nack(requeue: false)` direto para a DLQ.
- A política fica em `TransactionRegisteredMessageHandler`, isolada do transporte, o que permite testá-la sem broker.

**Consequências.** (+) Nenhuma perda silenciosa; DLQ auditável e reprocessável. (+) Picos de escrita ficam represados na fila em vez de sobrecarregar o banco.

---

## ADR-005: Estratégia para 50 req/s com no máximo 5% de perda no Consolidado

**Contexto.** Em picos, o consolidado recebe 50 req/s e pode perder no máximo 5% delas.

**Decisão (defesa em camadas).**
1. **Read model materializado:** a consulta do dia é uma busca por chave primária; o relatório é um *range scan* na PK mais uma soma para o saldo de abertura.
2. **Cache em memória** (Decorator sobre `IDailyBalanceReader`, TTL configurável de 5s): em pico, a maioria das requisições nem chega ao banco.
3. **Pool de conexões** dimensionado (`Maximum Pool Size=50`) e consultas `AsNoTracking`.
4. **Rate limiting** (fixed window, 200 req/s com fila de 50): acima da capacidade a API **rejeita rápido com 429** em vez de acumular latência até cair (*load shedding*). O limite padrão dá 4x de folga sobre o pico exigido.
5. **Escrita isolada da leitura:** o consumo de eventos tem prefetch limitado, então a carga de escrita é suavizada pela fila.
6. **Medição:** script k6 com `constant-arrival-rate` e *thresholds* explícitos (`http_req_failed < 5%`, `p95 < 300ms`).

**Escala horizontal.** As APIs são *stateless*; com N réplicas, o cache passa a ser Redis (`IDistributedCache`) sem mudança nos casos de uso, graças ao Decorator.

---

## ADR-006: Result Pattern e ProblemDetails

**Decisão.** Regras de negócio retornam `Result`/`Result<T>` com `Error(Code, Message, Type)`. A camada web traduz `ErrorType` para status HTTP e devolve **ProblemDetails (RFC 7807)** com um `code` estável e `traceId`. Exceções ficam reservadas para o inesperado e são tratadas por `IExceptionHandler` (500 sem vazar detalhes).

**Consequências.** (+) Fluxo explícito e testável, sem `try/catch` para controle de fluxo. (+) O front-end pode tratar erros pelo `code`, independente do texto.

---

## ADR-007: CQRS sem MediatR

**Decisão.** Interfaces `ICommandHandler<TCommand, TResponse>` e `IQueryHandler<TQuery, TResponse>` registradas diretamente no DI e injetadas nos endpoints.

**Motivo.** Para o tamanho do problema, um mediador traria indireção sem benefício real. Além disso, MediatR (e MassTransit) passaram a ter licenciamento comercial em 2025. Comportamentos transversais (log, métricas, validação) podem ser adicionados com Decorators.

---

## ADR-008: Domínio rico e relógio de negócio

**Decisão.**
- `Transaction.Register(...)` e `Money.Create(...)` são *factory methods* que retornam `Result`: **não existe instância inválida**.
- O tipo (crédito/débito) define o sinal, e o valor é sempre positivo, o que evita ambiguidade de "débito negativo".
- `IBusinessClock` fornece "hoje" no fuso do lojista (`America/Sao_Paulo`), construído sobre o `TimeProvider` do .NET 8. Sem isso, um lançamento às 22h no Brasil cairia no dia seguinte (UTC).

---

## ADR-009: Schema via `EnsureCreated` (somente para o desafio)

**Decisão.** Para o avaliador rodar com um único `docker compose up`, cada serviço cria seu banco e schema na subida, com retry enquanto o PostgreSQL inicializa.

**Em produção:** migrations versionadas do EF Core (*migrations bundle*) aplicadas por um passo dedicado do pipeline, nunca pela aplicação em runtime.
