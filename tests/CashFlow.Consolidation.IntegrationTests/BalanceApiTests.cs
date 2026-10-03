using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CashFlow.Consolidation.Application.Balances.GetDaily;
using CashFlow.Consolidation.Domain;
using CashFlow.SharedKernel;

namespace CashFlow.Consolidation.IntegrationTests;

[Collection(ConsolidationApiCollection.Name)]
public sealed class BalanceApiTests(ConsolidationApiFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task GetDaily_ShouldReturnConsolidatedBalance()
    {
        var day = ConsolidationApiFixture.UniqueDay();
        await fixture.ApplyAsync(day, TransactionType.Credit, 500m);
        await fixture.ApplyAsync(day, TransactionType.Debit, 120m);

        var balance = await _client.GetFromJsonAsync<DailyBalanceResponse>($"/api/v1/daily-balances/{day:yyyy-MM-dd}", Json);

        Assert.Equal(day, balance!.Date);
        Assert.Equal(500m, balance.TotalCredits);
        Assert.Equal(120m, balance.TotalDebits);
        Assert.Equal(380m, balance.NetBalance);
    }

    [Fact]
    public async Task GetDaily_DayWithoutMovements_ShouldReturnZero()
    {
        var day = ConsolidationApiFixture.UniqueDay();

        var balance = await _client.GetFromJsonAsync<DailyBalanceResponse>($"/api/v1/daily-balances/{day:yyyy-MM-dd}", Json);

        Assert.Equal(0m, balance!.NetBalance);
    }

    [Fact]
    public async Task GetDaily_InvalidDate_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/v1/daily-balances/31-02-2026");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReport_ShouldReturnOneLinePerDayWithRunningBalance()
    {
        var from = ConsolidationApiFixture.UniqueDay();
        await fixture.ApplyAsync(from, TransactionType.Credit, 1000m);
        await fixture.ApplyAsync(from.AddDays(2), TransactionType.Debit, 250m);

        var report = await _client.GetFromJsonAsync<BalanceReport>(
            $"/api/v1/daily-balances?from={from:yyyy-MM-dd}&to={from.AddDays(2):yyyy-MM-dd}", Json);

        Assert.Equal(3, report!.Days.Count);
        Assert.Equal(1000m, report.TotalCredits);
        Assert.Equal(250m, report.TotalDebits);
        Assert.Equal(report.OpeningBalance + 750m, report.ClosingBalance);
        Assert.Equal(report.OpeningBalance + 1000m, report.Days[1].ClosingBalance);
    }

    [Fact]
    public async Task GetReport_InvertedPeriod_ShouldReturn400WithCode()
    {
        var response = await _client.GetAsync("/api/v1/daily-balances?from=2026-09-30&to=2026-09-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("report.period.invalid", problem.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Health_ShouldBeHealthy()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health/ready")).StatusCode);
    }
}
