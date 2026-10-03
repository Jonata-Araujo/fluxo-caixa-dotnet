using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using CashFlow.Consolidation.Api.Endpoints;
using CashFlow.Consolidation.Application;
using CashFlow.Consolidation.Infrastructure;
using CashFlow.Consolidation.Infrastructure.Persistence;
using CashFlow.Web.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCommonWeb()
    .AddConsolidationApplication()
    .AddConsolidationInfrastructure();

// Proteção contra sobrecarga (load shedding): acima da capacidade configurada, a API responde
// 429 rapidamente em vez de enfileirar requisições até derrubar o banco. Requisito: suportar
// picos de 50 req/s tolerando até 5% de perda – o limite padrão (200 req/s) dá folga de 4x.
var rateLimit = builder.Configuration.GetSection("RateLimiting");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter(BalanceEndpoints.RateLimitPolicy, limiter =>
    {
        limiter.PermitLimit = rateLimit.GetValue("PermitLimit", 200);
        limiter.Window = TimeSpan.FromSeconds(1);
        limiter.QueueLimit = rateLimit.GetValue("QueueLimit", 50);
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "CashFlow - Consolidado Diário", Version = "v1" }));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapBalanceEndpoints();
app.MapHealthEndpoints();

await app.Services.InitializeConsolidationDatabaseAsync();

await app.RunAsync();

// Necessário para WebApplicationFactory<Program> nos testes de integração.
public partial class Program { }
