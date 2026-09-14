using Jewel.JPMS.Contracts.Xero;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// One bill, whole, for list_xero_ledger_lines (2026-09-14, the bookkeeper's ask): every stored
/// line of it whatever tab each sits on, largest first, with the reference Xero holds beyond the
/// invoice number — the Invoice document window's bill card, so the model reads the note she
/// typed in Dext (who uploaded a receipt, what it was for) wherever it landed on the bill.
/// </summary>
internal static partial class AiFinanceTools
{
    private static async Task<string> BillAsync(
        AiToolContext context, string xeroInvoiceId, bool viewerMayHandleUnplaced, CancellationToken ct)
    {
        var lines = await context.Services
            .GetRequiredService<IQueryHandler<ListXeroLedgerLinesForInvoice, IReadOnlyList<XeroLedgerLine>>>()
            .HandleAsync(new ListXeroLedgerLinesForInvoice(xeroInvoiceId), ct);
        if (lines.Count == 0) return Fail("The portal holds no lines for that Xero invoice id — check the id against list_xero_ledger_lines, or Sync from Xero.");

        var first = lines[0];
        return Serialise(new
        {
            ok = true,
            xeroInvoiceId,
            first.InvoiceNumber,
            first.Reference,
            first.ContactName,
            first.Date,
            xeroStatus = first.InvoiceStatus,
            count = lines.Count,
            storedNet = lines.Sum(line => line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net),
            lines = lines.Select(line => Line(line, viewerMayHandleUnplaced)),
            note = "Stored lines only — the cost-of-sales lines the sync keeps — so storedNet is not the bill's total "
                 + "when an overhead line sits on the same bill. The bookkeeper's Dext description lands on a line's "
                 + "description or in reference; read every line before saying a bill carries no note."
        });
    }
}
