using CashFlow.Transactions.Api.Endpoints;
using CashFlow.Transactions.Application;
using CashFlow.Transactions.Infrastructure;
using CashFlow.Transactions.Infrastructure.Persistence;
using CashFlow.Web.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCommonWeb()
    .AddTransactionsApplication()
    .AddTransactionsInfrastructure();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new() { Title = "CashFlow - Lançamentos", Version = "v1" }));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapTransactionEndpoints();
app.MapHealthEndpoints();

await app.Services.InitializeTransactionsDatabaseAsync();

await app.RunAsync();

// Necessário para WebApplicationFactory<Program> nos testes de integração.
public partial class Program { }
