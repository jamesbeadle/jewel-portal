using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.XeroPayments;

/// <summary>
/// One rule for what Xero's sales invoices mean for a project's valuation invoices — the preview a
/// person confirms, the sync that follows and the nightly worker's unattended run all read it, so
/// what was shown is what is done (2026-09-11). Reads the project's mapped Xero contact's invoices
/// FRESH (every status — PAID is the point), then for every ISSUED portal invoice: a linked one is
/// followed to its Xero row (PAID there → record the payment here); an unlinked one is matched the
/// way a person would — Xero's reference or number naming the valuation, else the same net amount
/// — and linked when the match is unique. Two candidates are never guessed between. Raised /
/// Submitted / Approved are not yet certified and are left alone; Paid and Cancelled are done.
/// </summary>
internal sealed class ValuationInvoicePaymentSyncPlanner
{
    /// <summary>A net that agrees to the penny — Xero rounds VAT per line, the portal holds a figure.</summary>
    internal const decimal AmountTolerance = 0.01m;

    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public ValuationInvoicePaymentSyncPlanner(JpmsContext context, IXeroClient xero)
    {
        this.context = context;
        this.xero = xero;
    }

    public async Task<ValuationInvoicePaymentSyncPlan> PlanAsync(string projectId, CancellationToken ct)
    {
        var project = await context.Projects.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ProjectId == projectId, ct)
            ?? throw new InvalidOperationException($"Project {projectId} not found.");
        // The contact mapped on the project, by id only — the same source the raise uses
        // (ValuationInvoiceXeroRaiseSources.MappedContactOf, inlined so the worker links one file fewer).
        var contactId = string.IsNullOrWhiteSpace(project.XeroContactId) ? null : project.XeroContactId.Trim();
        var contactName = contactId is not null && !string.IsNullOrWhiteSpace(project.XeroContactName)
            ? project.XeroContactName.Trim()
            : (project.ClientName ?? "").Trim();

        var blockers = new List<string>();
        var snapshot = XeroSalesInvoiceListSnapshot.NotConfigured();
        if (contactId is null)
            blockers.Add($"No Xero contact is mapped on {project.Name} — set it in Project settings (Xero contact) so its sales invoices can be read.");
        else
        {
            snapshot = await xero.GetSalesInvoicesForContactAsync(contactId, ct);
            if (!snapshot.IsConfigured) blockers.Add("Xero is not connected on this portal — nothing can be read.");
            else if (snapshot.Error is not null) blockers.Add($"Xero could not be read: {snapshot.Error}");
        }

        var invoices = await context.ValuationInvoices.AsNoTracking()
            .Where(row => row.ProjectId == projectId)
            .OrderBy(row => row.Number)
            .ToListAsync(ct);

        var rows = blockers.Count == 0
            ? Plan(invoices, snapshot.Invoices)
            : Array.Empty<ValuationInvoicePaymentSyncRow>();

        return new ValuationInvoicePaymentSyncPlan(project, contactId, contactName, snapshot.FetchedAtUtc, rows, blockers);
    }

    /// <summary>The rows, one per Issued portal invoice, in number order. Pure — tested on its own.</summary>
    internal static IReadOnlyList<ValuationInvoicePaymentSyncRow> Plan(
        IReadOnlyList<ValuationInvoiceEntity> invoices, IReadOnlyList<XeroSalesInvoiceSummary> xeroInvoices)
    {
        // A Xero invoice already held by ANY portal invoice (paid ones included) is spoken for.
        var claimedIds = invoices.Where(row => !string.IsNullOrWhiteSpace(row.XeroInvoiceId))
            .Select(row => row.XeroInvoiceId!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var claimedNumbers = invoices.Where(row => !string.IsNullOrWhiteSpace(row.XeroInvoiceNumber))
            .Select(row => row.XeroInvoiceNumber!.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var issued = invoices.Where(row => row.Status == (int)ValuationInvoiceStatus.Issued).ToList();

        // Candidates for every unlinked invoice are found FIRST, so a Xero row that would be the
        // sole match of two portal invoices (two months at the same figure) is ambiguous for
        // both — never handed to whichever came first.
        var candidatesByInvoice = issued
            .Where(row => !IsLinked(row))
            .ToDictionary(row => row.ValuationInvoiceId, row => CandidatesFor(row, xeroInvoices, claimedIds, claimedNumbers));

        var rows = new List<ValuationInvoicePaymentSyncRow>();
        foreach (var invoice in issued)
        {
            rows.Add(IsLinked(invoice)
                ? PlanLinked(invoice, xeroInvoices)
                : PlanUnlinked(invoice, candidatesByInvoice, issued));
        }
        return rows;
    }

    private static bool IsLinked(ValuationInvoiceEntity invoice) =>
        !string.IsNullOrWhiteSpace(invoice.XeroInvoiceId) || !string.IsNullOrWhiteSpace(invoice.XeroInvoiceNumber);

    /// <summary>A linked invoice is followed by Xero's id first, else by its number.</summary>
    private static ValuationInvoicePaymentSyncRow PlanLinked(ValuationInvoiceEntity invoice, IReadOnlyList<XeroSalesInvoiceSummary> xeroInvoices)
    {
        var xeroRow = FindById(xeroInvoices, invoice.XeroInvoiceId) ?? FindByNumber(xeroInvoices, invoice.XeroInvoiceNumber);
        var handle = string.IsNullOrWhiteSpace(invoice.XeroInvoiceNumber) ? invoice.XeroInvoiceId : invoice.XeroInvoiceNumber;
        if (xeroRow is null)
            return NoChange(invoice, invoice.XeroInvoiceId, invoice.XeroInvoiceNumber, null,
                $"Xero no longer holds {handle} on the project's contact — check the number, or the contact mapping.");
        return PaymentRowFor(invoice, xeroRow, link: false);
    }

    /// <summary>What Xero's row means for the portal invoice it is (or would be) linked to.</summary>
    private static ValuationInvoicePaymentSyncRow PaymentRowFor(ValuationInvoiceEntity invoice, XeroSalesInvoiceSummary xeroRow, bool link)
    {
        var linkNote = link ? $"Matched to {xeroRow.Number ?? xeroRow.InvoiceId} ({MatchReason(invoice, xeroRow)}). " : "";
        if (xeroRow.IsVoided)
            return NoChange(invoice, xeroRow.InvoiceId, xeroRow.Number, xeroRow.Status,
                "VOIDED in Xero — nothing recorded; record the re-issued invoice's number if there is one.", xeroDate: xeroRow.Date);
        if (xeroRow.IsPaid)
        {
            var netNote = Math.Abs(xeroRow.SubTotal - invoice.Amount) > AmountTolerance
                ? $"Xero net £{xeroRow.SubTotal:N2} differs from the portal's £{invoice.Amount:N2} — the portal's figure is recorded; check which is right. "
                : "";
            var paidOn = xeroRow.FullyPaidOnDate?.Date ?? DateTime.UtcNow.Date;
            return new ValuationInvoicePaymentSyncRow(
                invoice.ValuationInvoiceId, invoice.Reference, ValuationInvoiceStatus.Issued, invoice.Amount,
                xeroRow.InvoiceId, xeroRow.Number, xeroRow.Status, xeroRow.Date,
                link ? ValuationInvoicePaymentSyncAction.LinkAndRecordPayment : ValuationInvoicePaymentSyncAction.RecordPayment,
                invoice.Amount, paidOn,
                Trim($"{linkNote}{netNote}Paid in Xero £{xeroRow.AmountPaid:N2} (gross){(xeroRow.FullyPaidOnDate is { } d ? $" on {d:dd MMM yyyy}" : ", date not given — today is used")}."),
                Array.Empty<string>());
        }
        if (xeroRow.IsPartPaid)
            return WithLink(invoice, xeroRow, link, $"{linkNote}Part paid in Xero: £{xeroRow.AmountPaid:N2} of £{xeroRow.Total:N2} — nothing recorded until it is settled.");
        return WithLink(invoice, xeroRow, link, $"{linkNote}Unpaid in Xero ({xeroRow.Status}{(xeroRow.DueDate is { } due ? $", due {due:dd MMM yyyy}" : "")}).");
    }

    /// <summary>Unpaid or part paid: the unique match is still linked so the next sync follows it by id.</summary>
    private static ValuationInvoicePaymentSyncRow WithLink(ValuationInvoiceEntity invoice, XeroSalesInvoiceSummary xeroRow, bool link, string note) =>
        link
            ? new ValuationInvoicePaymentSyncRow(
                invoice.ValuationInvoiceId, invoice.Reference, ValuationInvoiceStatus.Issued, invoice.Amount,
                xeroRow.InvoiceId, xeroRow.Number, xeroRow.Status, xeroRow.Date, ValuationInvoicePaymentSyncAction.Link, null, null,
                Trim(note), Array.Empty<string>())
            : NoChange(invoice, xeroRow.InvoiceId, xeroRow.Number, xeroRow.Status, Trim(note), xeroDate: xeroRow.Date);

    private static ValuationInvoicePaymentSyncRow PlanUnlinked(
        ValuationInvoiceEntity invoice,
        IReadOnlyDictionary<string, List<XeroSalesInvoiceSummary>> candidatesByInvoice,
        IReadOnlyList<ValuationInvoiceEntity> issued)
    {
        var candidates = candidatesByInvoice[invoice.ValuationInvoiceId];
        if (candidates.Count == 0)
            return NoChange(invoice, null, null, null, $"No Xero invoice matches (net £{invoice.Amount:N2}, {invoice.Reference}) — record its Xero number by hand if it was raised under another figure.");

        if (candidates.Count == 1)
        {
            var only = candidates[0];
            var rivals = issued
                .Where(other => other.ValuationInvoiceId != invoice.ValuationInvoiceId
                    && candidatesByInvoice.TryGetValue(other.ValuationInvoiceId, out var theirs)
                    && theirs.Any(row => row.InvoiceId == only.InvoiceId))
                .Select(other => other.Reference)
                .ToList();
            if (rivals.Count == 0) return PaymentRowFor(invoice, only, link: true);
            return NoChange(invoice, null, null, null,
                $"Ambiguous: {Describe(only)} also matches {string.Join(", ", rivals)} — link the right one with record_valuation_invoice_xero_number, then sync again.",
                new[] { Describe(only) });
        }

        return NoChange(invoice, null, null, null,
            $"Ambiguous: {candidates.Count} Xero invoices match — link one with record_valuation_invoice_xero_number, then sync again.",
            candidates.Select(Describe).ToList());
    }

    /// <summary>
    /// The Xero rows a person would take for this valuation invoice: unclaimed, live (not voided),
    /// and either NAMED for it — the reference or number carries "VI-0005" or "Valuation 05" /
    /// "Valuation 5" — or the same net to the penny.
    /// </summary>
    private static List<XeroSalesInvoiceSummary> CandidatesFor(
        ValuationInvoiceEntity invoice, IReadOnlyList<XeroSalesInvoiceSummary> xeroInvoices,
        HashSet<string> claimedIds, HashSet<string> claimedNumbers)
    {
        var matching = xeroInvoices
            .Where(row => !row.IsVoided)
            .Where(row => !claimedIds.Contains(row.InvoiceId)
                && (string.IsNullOrWhiteSpace(row.Number) || !claimedNumbers.Contains(row.Number.Trim())))
            .Where(row => NamesInvoice(row, invoice) || SameNet(row, invoice))
            .ToList();
        // A row NAMED for the valuation outranks one that merely carries the same figure — two
        // months at the same net, each with "Valuation NN" in Xero, pair off exactly as a person
        // would read them. Only when nothing is named does the figure alone decide.
        var named = matching.Where(row => NamesInvoice(row, invoice)).ToList();
        return named.Count > 0 ? named : matching;
    }

    internal static bool NamesInvoice(XeroSalesInvoiceSummary row, ValuationInvoiceEntity invoice) =>
        NamesInvoice(row.Reference, invoice) || NamesInvoice(row.Number, invoice);

    /// <summary>"VI-0005" anywhere, or the word Valuation followed by the number (leading zeros
    /// optional, whole number only — "Valuation 5" is not "Valuation 50").</summary>
    internal static bool NamesInvoice(string? text, ValuationInvoiceEntity invoice)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (!string.IsNullOrWhiteSpace(invoice.Reference)
            && text.Contains(invoice.Reference.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        return invoice.Number > 0
            && Regex.IsMatch(text, $@"\bvaluation\s*0*{invoice.Number}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    internal static bool SameNet(XeroSalesInvoiceSummary row, ValuationInvoiceEntity invoice) =>
        invoice.Amount > 0m && Math.Abs(row.SubTotal - invoice.Amount) <= AmountTolerance;

    private static string MatchReason(ValuationInvoiceEntity invoice, XeroSalesInvoiceSummary row) =>
        NamesInvoice(row, invoice)
            ? SameNet(row, invoice) ? "reference names it and the net agrees" : "reference names it"
            : $"same net £{row.SubTotal:N2}";

    private static string Describe(XeroSalesInvoiceSummary row) =>
        $"{row.Number ?? row.InvoiceId}"
        + (string.IsNullOrWhiteSpace(row.Reference) ? "" : $" \"{row.Reference}\"")
        + $" net £{row.SubTotal:N2}, {row.Status}"
        + (row.Date is { } date ? $", dated {date:dd MMM yyyy}" : "");

    private static XeroSalesInvoiceSummary? FindById(IReadOnlyList<XeroSalesInvoiceSummary> rows, string? id) =>
        string.IsNullOrWhiteSpace(id) ? null
            : rows.FirstOrDefault(row => string.Equals(row.InvoiceId, id.Trim(), StringComparison.OrdinalIgnoreCase));

    private static XeroSalesInvoiceSummary? FindByNumber(IReadOnlyList<XeroSalesInvoiceSummary> rows, string? number) =>
        string.IsNullOrWhiteSpace(number) ? null
            : rows.FirstOrDefault(row => string.Equals(row.Number, number.Trim(), StringComparison.OrdinalIgnoreCase));

    private static ValuationInvoicePaymentSyncRow NoChange(
        ValuationInvoiceEntity invoice, string? xeroId, string? xeroNumber, string? xeroStatus, string note,
        IReadOnlyList<string>? candidates = null, DateTime? xeroDate = null) =>
        new(invoice.ValuationInvoiceId, invoice.Reference, ValuationInvoiceStatus.Issued, invoice.Amount,
            xeroId, xeroNumber, xeroStatus, xeroDate, ValuationInvoicePaymentSyncAction.None, null, null, note,
            candidates ?? Array.Empty<string>());

    private static string Trim(string note) => note.Trim();
}

/// <summary>Everything the preview shows and the sync applies.</summary>
internal sealed record ValuationInvoicePaymentSyncPlan(
    ProjectEntity Project,
    string? XeroContactId,
    string XeroContactName,
    DateTimeOffset? FetchedAtUtc,
    IReadOnlyList<ValuationInvoicePaymentSyncRow> Rows,
    IReadOnlyList<string> Blockers)
{
    public ValuationInvoicePaymentSyncPreview ToPreview() => new(
        Project.ProjectId, Project.Name, XeroContactId,
        XeroContactId is null ? null : XeroContactName, FetchedAtUtc, Rows, Blockers,
        LinksPlanned: Rows.Count(row => row.Links),
        PaymentsPlanned: Rows.Count(row => row.RecordsPayment),
        NoChange: Rows.Count(row => row.Action == ValuationInvoicePaymentSyncAction.None));
}
