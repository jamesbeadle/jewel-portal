using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{

    private async Task<XeroCashSummarySnapshot> FetchCashSummaryAsync(CancellationToken ct)
    {
        string token;
        try
        {
            token = await GetAccessTokenAsync(ct);
        }
        catch (XeroCallFailedException tokenFailure)
        {
            return XeroCashSummarySnapshot.Failed(tokenFailure.Message);
        }

        try
        {
            var bankAccounts = await FetchBankBalancesAsync(token, ct);
            var outstanding = await FetchOutstandingSalesInvoicesAsync(token, ct);
            return new XeroCashSummarySnapshot(true, null, DateTimeOffset.UtcNow, bankAccounts, outstanding);
        }
        catch (XeroCallFailedException callFailure)
        {
            return XeroCashSummarySnapshot.Failed(callFailure.Message);
        }
    }

    /// <summary>
    /// Each bank account's closing balance as of today, from Xero's bank summary report
    /// (the report is in the organisation's base currency). The report's rows carry the
    /// account name + accountID in the first cell and the closing balance in the column the
    /// header names "Closing Balance" (last column as a fallback, so a report-layout tweak
    /// degrades gracefully rather than dropping balances).
    /// </summary>
    private async Task<IReadOnlyList<XeroBankAccountBalance>> FetchBankBalancesAsync(string token, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var url = $"{BankSummaryReportUrl}?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}";

        JsonDocument doc;
        try
        {
            doc = await GetJsonAsync(token, url, "bank summary report", ct);
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 403"))
        {
            throw new XeroCallFailedException(
                "Couldn't read Xero's bank summary report — the Xero custom connection needs the "
                + "accounting.reports.read scope ticked in the Xero developer portal. " + failure.Message);
        }

        using (doc)
        {
            var balances = new List<XeroBankAccountBalance>();
            if (!doc.RootElement.TryGetProperty("Reports", out var reports)
                || reports.ValueKind != JsonValueKind.Array || reports.GetArrayLength() == 0)
                return balances;

            var closingColumn = -1;
            foreach (var row in RowsOf(reports[0]))
            {
                var rowType = StringOf(row, "RowType");
                if (rowType == "Header")
                {
                    closingColumn = FindColumn(row, "Closing Balance");
                    continue;
                }
                if (rowType != "Section") continue;

                foreach (var accountRow in RowsOf(row))
                {
                    if (StringOf(accountRow, "RowType") != "Row") continue;
                    if (!accountRow.TryGetProperty("Cells", out var cells)
                        || cells.ValueKind != JsonValueKind.Array || cells.GetArrayLength() == 0)
                        continue;

                    var nameCell = cells[0];
                    var name = StringOf(nameCell, "Value");
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var balanceIndex = closingColumn >= 0 && closingColumn < cells.GetArrayLength()
                        ? closingColumn
                        : cells.GetArrayLength() - 1;
                    balances.Add(new XeroBankAccountBalance(
                        AccountId: CellAttribute(nameCell, "accountID") ?? name,
                        Name: name,
                        Balance: CellDecimal(cells[balanceIndex])));
                }
            }
            return balances;
        }
    }

    /// <summary>Rows of a report or section — both nest them under "Rows".</summary>
    private static IEnumerable<JsonElement> RowsOf(JsonElement reportOrSection)
    {
        if (reportOrSection.TryGetProperty("Rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
            foreach (var row in rows.EnumerateArray())
                yield return row;
    }

    private static int FindColumn(JsonElement headerRow, string title)
    {
        if (!headerRow.TryGetProperty("Cells", out var cells) || cells.ValueKind != JsonValueKind.Array)
            return -1;
        var index = 0;
        foreach (var cell in cells.EnumerateArray())
        {
            if (string.Equals(StringOf(cell, "Value"), title, StringComparison.OrdinalIgnoreCase))
                return index;
            index++;
        }
        return -1;
    }

    /// <summary>Report cell values arrive as strings ("12345.67"); attributes as [{ Id, Value }].</summary>
    private static decimal CellDecimal(JsonElement cell) =>
        decimal.TryParse(StringOf(cell, "Value"), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : 0m;

    private static string? CellAttribute(JsonElement cell, string id)
    {
        if (!cell.TryGetProperty("Attributes", out var attributes) || attributes.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var attribute in attributes.EnumerateArray())
            if (string.Equals(StringOf(attribute, "Id"), id, StringComparison.OrdinalIgnoreCase))
                return StringOf(attribute, "Value");
        return null;
    }
}
