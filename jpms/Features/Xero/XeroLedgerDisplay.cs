
namespace Jewel.JPMS.Features.Xero;

/// <summary>How a ledger line reads on screen — shared by the allocation page and its row
/// components and dialogs so the two can never disagree about a credit note, a draft bill,
/// or which month a labour bill settles.</summary>
public static class XeroLedgerDisplay
{
    /// <summary>A credit note subtracts: ACCPAYCREDIT lines carry their net negated.</summary>
    public static decimal SignedNet(XeroLedgerLine line) =>
        line.Type == "ACCPAYCREDIT" ? -line.Net : line.Net;

    /// <summary>Draft or submitted in Xero — allocating every line of the bill approves it.</summary>
    public static bool IsAwaitingApproval(XeroLedgerLine line) =>
        line.InvoiceStatus.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
        || line.InvoiceStatus.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase);

    /// <summary>Date, invoice number, Xero site and Xero code on one small line under the
    /// description — what used to be the Date column and the description's footnote.</summary>
    public static string LineMetaText(XeroLedgerLine line) =>
        $"{DateText(line.Date)} · {line.InvoiceNumber ?? "—"} · {line.XeroSite ?? "no site"} · {line.XeroCostCode ?? "no Xero code"}";

    /// <summary>The month a labour bill settles: its bill date's month.</summary>
    public static DateTimeOffset? SettlementMonthOf(XeroLedgerLine line) =>
        line.Date is { } date
            // date.Year/Month only — never the DateTime itself, whose Kind would make the
            // offset constructor throw for Local kinds (the BST lesson).
            ? new DateTimeOffset(new DateTime(date.Year, date.Month, 1), TimeSpan.Zero)
            : null;

    /// <summary>Covering reconciles by settlement counterparty against a bill month — both must exist.</summary>
    public static bool CanMarkCover(XeroLedgerLine line) =>
        line.MatchedSubcontractorId is not null && line.Date is not null;

    public static string MarkCoverHint(XeroLedgerLine line) =>
        line.MatchedSubcontractorId is null
            ? "Covering reconciles by settlement counterparty — link a company or flag the worker a sole trader (inline, left) first"
            : line.Date is null
                ? "The line has no bill date, so there is no month to settle against"
                : $"Mark this line as settlement of {line.MatchedWorkerName}'s {SettlementMonthOf(line):MMMM yyyy} timesheets — covered value is excluded from cost-of-sales aggregations; the worker-month verdict lives on the Labour overview's Settlement view";
}
