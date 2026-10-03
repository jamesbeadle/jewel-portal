using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Xero.Commands;
using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>A keyed statement balance stands until a newer statement arrives, and is held to its
/// columns at the service before anything is stored.</summary>
public sealed class KeyedBankStatementBalanceTests
{
    private const string LloydsId = "lloyds-1";
    private static readonly DateTimeOffset SecondOfOctober = new(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FirstOfOctober = SecondOfOctober.AddDays(-1);

    [Fact]
    public void AKeyedBalanceReplacesXerosBalanceForItsAccount()
    {
        var cash = Snapshot(new XeroBankAccountBalance(LloydsId, "Lloyds", 107899.50m));

        var keyed = cash.WithKeyedStatements(new[] { Keyed(5718.66m, SecondOfOctober) });

        var lloyds = Assert.Single(keyed.BankAccounts);
        Assert.Equal(5718.66m, keyed.TotalCash);
        Assert.Equal(SecondOfOctober, lloyds.StatementDate);
        var statement = lloyds.Statement!;
        Assert.Equal(BankStatementSource.Keyed, statement.Source);
    }

    [Fact]
    public void ANewerStatementFromXeroOutranksAnOlderKeyedOne()
    {
        var xeroStatement = new BankStatementBalance(6000m, SecondOfOctober, BankStatementSource.Xero);
        var cash = Snapshot(new XeroBankAccountBalance(LloydsId, "Lloyds", 107899.50m, xeroStatement));

        var keyed = cash.WithKeyedStatements(new[] { Keyed(5718.66m, FirstOfOctober) });

        Assert.Equal(6000m, keyed.TotalCash);
    }

    [Fact]
    public void TheServiceRefusesPartPenniesAFutureDateAndAMissingAccount()
    {
        var tomorrow = SiteClock.Today().AddDays(1);
        var command = new KeyBankStatementBalance("", "Lloyds", 5718.665m, tomorrow);

        var outcome = new KeyBankStatementBalanceValidation().Check(command);

        Assert.Equal(3, outcome.Errors.Count);
        Assert.Contains(outcome.Errors, error => error.StartsWith("accountId"));
        Assert.Contains(outcome.Errors, error => error.StartsWith("balance must be in whole pence"));
        Assert.Contains(outcome.Errors, error => error.StartsWith("statementDate cannot be later"));
    }

    [Fact]
    public void TheServicePassesAnOverdrawnBalanceKeyedForToday()
    {
        var command = new KeyBankStatementBalance(LloydsId, "Lloyds", -1250.40m, SiteClock.Today());

        Assert.False(new KeyBankStatementBalanceValidation().Check(command).HasFailed);
    }

    private static XeroCashSummarySnapshot Snapshot(params XeroBankAccountBalance[] accounts) =>
        new(true, null, DateTimeOffset.UtcNow, accounts, Array.Empty<XeroOutstandingSalesInvoice>());

    private static KeyedBankStatementBalance Keyed(decimal balance, DateTimeOffset statementDate) =>
        new(LloydsId, "Lloyds", balance, statementDate, "nigel@example.com", DateTimeOffset.UtcNow);
}
