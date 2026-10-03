namespace CashFlow.SharedKernel;

/*
 * CQRS "leve" sem dependência de MediatR (que passou a ter licença comercial em 2025).
 * Os handlers são resolvidos diretamente via DI: explícito, sem reflexão e fácil de testar.
 * Comportamentos transversais (log, métricas, retry) podem ser adicionados como Decorators.
 */

public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

/// <summary>Página de resultados.</summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalItems)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
