using System.Net;
using System.Text;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// Cash in bank is the statement balance, not "Balance in Xero" (2026-10-02: Lloyds read
/// £5,718.66 on the statement against £107,899.50 in Xero). The real client against a fake
/// Xero: the bank summary gives each account's Xero balance, cash validation its statement.
/// </summary>
public sealed class BankStatementBalanceReadTests
{
    private const string LloydsId = "lloyds-1";
    private const string CardId = "card-1";

    [Fact]
    public async Task EachAccountCarriesItsStatementBalanceAndTheTotalCountsTheStatements()
    {
        var xero = new FakeXero(CashValidation(
            Account(LloydsId, 5718.66m, "DEBIT", "2026-10-02"),
            Account(CardId, 250m, "CREDIT", "2026-10-01")));

        var cash = await xero.Client.GetCashSummaryAsync(force: true, CancellationToken.None);

        Assert.Null(cash.StatementError);
        var lloyds = cash.BankAccounts.Single(account => account.AccountId == LloydsId);
        Assert.Equal(107899.50m, lloyds.Balance);
        Assert.Equal(5718.66m, lloyds.CashInBank);
        var statement = lloyds.Statement!;
        Assert.Equal(new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero), statement.StatementDate);
        Assert.Equal(BankStatementSource.Xero, statement.Source);
        Assert.Equal(5718.66m - 250m, cash.TotalCash);
        Assert.All(cash.BankAccounts, account => Assert.True(account.HasStatement));
    }

    [Fact]
    public async Task ARefusedStatementReadKeepsXerosBalanceAndSaysWhy()
    {
        var xero = new FakeXero(cashValidation: null);

        var cash = await xero.Client.GetCashSummaryAsync(force: true, CancellationToken.None);

        Assert.Null(cash.Error);
        Assert.Contains("finance.cashvalidation.read", cash.StatementError);
        Assert.Equal(107899.50m - 11465.77m, cash.TotalCash);
        Assert.All(cash.BankAccounts, account => Assert.False(account.HasStatement));
    }

    private static string CashValidation(params string[] accounts) => $"[{string.Join(",", accounts)}]";

    private static string Account(string accountId, decimal value, string type, string date) =>
        $$"""{"accountId":"{{accountId}}","statementBalance":{"value":{{value}},"type":"{{type}}"},"statementBalanceDate":"{{date}}"}""";

    private const string BankSummary = """
        {"Reports":[{"Rows":[
          {"RowType":"Header","Cells":[{"Value":"Bank Accounts"},{"Value":"Opening Balance"},{"Value":"Cash Received"},{"Value":"Cash Spent"},{"Value":"Closing Balance"}]},
          {"RowType":"Section","Rows":[
            {"RowType":"Row","Cells":[{"Value":"Lloyds","Attributes":[{"Id":"accountID","Value":"lloyds-1"}]},{"Value":"0"},{"Value":"0"},{"Value":"0"},{"Value":"107899.50"}]},
            {"RowType":"Row","Cells":[{"Value":"Card","Attributes":[{"Id":"accountID","Value":"card-1"}]},{"Value":"0"},{"Value":"0"},{"Value":"0"},{"Value":"-11465.77"}]}
          ]}
        ]}]}
        """;

    private sealed class FakeXero : HttpMessageHandler
    {
        private readonly string? cashValidation;

        public XeroClient Client { get; }

        public FakeXero(string? cashValidation)
        {
            this.cashValidation = cashValidation;
            var options = new XeroOptions { ClientId = "id", ClientSecret = "secret", CacheMinutes = 0 };
            Client = new XeroClient(new HttpClient(this), options, NullLogger<XeroClient>.Instance);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("identity.xero.com")) return Reply(HttpStatusCode.OK, """{"access_token":"token","expires_in":1800}""");
            if (url.Contains("Reports/BankSummary")) return Reply(HttpStatusCode.OK, BankSummary);
            if (url.Contains("CashValidation"))
                return cashValidation is null
                    ? Reply(HttpStatusCode.Forbidden, """{"title":"Forbidden"}""")
                    : Reply(HttpStatusCode.OK, cashValidation);
            if (url.Contains("/Invoices?")) return Reply(HttpStatusCode.OK, """{"Invoices":[]}""");
            throw new InvalidOperationException($"Unexpected Xero call {url}");
        }

        private static Task<HttpResponseMessage> Reply(HttpStatusCode status, string body) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
