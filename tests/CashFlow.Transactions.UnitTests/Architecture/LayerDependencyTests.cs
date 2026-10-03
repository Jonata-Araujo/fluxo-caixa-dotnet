using System.Reflection;
using CashFlow.Transactions.Application.Transactions.Register;
using CashFlow.Transactions.Domain;

namespace CashFlow.Transactions.UnitTests.Architecture;

/// <summary>
/// Testes de arquitetura: garantem que a regra de dependência da Clean Architecture
/// não seja violada com o tempo (o domínio não conhece infraestrutura nem frameworks).
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly string[] ForbiddenForInnerLayers =
    [
        "CashFlow.Transactions.Infrastructure",
        "CashFlow.Transactions.Api",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "RabbitMQ.Client",
        "Microsoft.AspNetCore"
    ];

    [Fact]
    public void Domain_ShouldNotDependOnOuterLayersOrFrameworks() =>
        AssertNoForbiddenReferences(typeof(Transaction).Assembly);

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrFrameworks() =>
        AssertNoForbiddenReferences(typeof(RegisterTransactionHandler).Assembly);

    private static void AssertNoForbiddenReferences(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToList();

        var violations = references
            .Where(reference => ForbiddenForInnerLayers.Any(forbidden => reference.StartsWith(forbidden, StringComparison.Ordinal)))
            .ToList();

        Assert.True(violations.Count == 0,
            $"{assembly.GetName().Name} não deveria referenciar: {string.Join(", ", violations)}");
    }
}
