using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CashFlow.SharedKernel;
using CashFlow.Transactions.Application.Transactions;
using CashFlow.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Transactions.IntegrationTests;

[Collection(TransactionsApiCollection.Name)]
public sealed class TransactionsApiTests(TransactionsApiFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task Post_ValidTransaction_ShouldReturn201AndBeRetrievable()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transactions",
            new { type = "credit", amount = 150.75m, description = "Venda balcão", date = "2026-09-29" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>(Json);
        Assert.NotNull(created);
        Assert.NotNull(response.Headers.Location);

        var fetched = await _client.GetFromJsonAsync<TransactionResponse>(response.Headers.Location, Json);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(150.75m, fetched.Amount);
        Assert.Equal("Credit", fetched.Type);
        Assert.Equal(new DateOnly(2026, 9, 29), fetched.Date);
    }

    [Theory]
    [InlineData("credit", "0", "Venda", "transaction.amount.non_positive")]
    [InlineData("credit", "10.123", "Venda", "transaction.amount.precision")]
    [InlineData("credit", "10", "", "transaction.description.required")]
    [InlineData("pix", "10", "Venda", "transaction.type.invalid")]
    public async Task Post_InvalidTransaction_ShouldReturn400ProblemDetailsWithErrorCode(
        string type, string rawAmount, string description, string expectedCode)
    {
        var amount = decimal.Parse(rawAmount, System.Globalization.CultureInfo.InvariantCulture);

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", new { type, amount, description });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedCode, problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Post_MalformedBody_ShouldReturn400()
    {
        using var content = new StringContent("{ \"amount\": \"abc\" }", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/v1/transactions", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownTransaction_ShouldReturn404()
    {
        var response = await _client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_ShouldReturnTransactionsOfTheDay()
    {
        const string date = "2026-08-15";
        await _client.PostAsJsonAsync("/api/v1/transactions", new { type = "credit", amount = 10m, description = "A", date });
        await _client.PostAsJsonAsync("/api/v1/transactions", new { type = "debit", amount = 4m, description = "B", date });

        var page = await _client.GetFromJsonAsync<PagedResponse<TransactionResponse>>(
            $"/api/v1/transactions?date={date}&pageSize=50", Json);

        Assert.Equal(2, page!.TotalItems);
        Assert.Equal(6m, page.Items.Sum(t => t.SignedAmount));
    }

    /// <summary>
    /// Requisito não funcional: o serviço de lançamentos não pode ficar indisponível se o
    /// consolidado (ou o broker) cair. Aqui o RabbitMQ está inacessível durante todo o teste.
    /// </summary>
    [Fact]
    public async Task Post_WithBrokerUnavailable_ShouldStillSucceedAndKeepEventPendingInOutbox()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/transactions",
            new { type = "debit", amount = 42m, description = "Broker fora do ar" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TransactionResponse>(Json);

        // O dispatcher tenta publicar, falha e registra a tentativa – sem perder o evento.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var attempts = 0;
        while (DateTime.UtcNow < deadline)
        {
            await using var scope = fixture.Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
            var pending = await db.OutboxMessages.AsNoTracking()
                .Where(m => m.ProcessedAt == null)
                .ToListAsync();

            var message = pending.SingleOrDefault(m => m.Payload.Contains(created!.Id.ToString()));
            Assert.NotNull(message);

            // O dispatcher interrompe o lote na primeira falha (preserva a ordem), então só a mensagem
            // pendente mais antiga recebe a tentativa – que pode ser de outro teste da mesma coleção.
            attempts = pending.Max(m => m.Attempts);
            if (attempts > 0)
            {
                break;
            }

            await Task.Delay(200);
        }

        Assert.True(attempts > 0, "O outbox deveria ter registrado ao menos uma tentativa de publicação.");
    }

    [Fact]
    public async Task HealthReady_ShouldBeHealthyEvenWithBrokerUnavailable()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
