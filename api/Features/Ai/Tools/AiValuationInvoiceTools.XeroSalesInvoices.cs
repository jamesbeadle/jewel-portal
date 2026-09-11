using Jewel.JPMS.Api.Features.ValuationInvoices;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The sales side read BACK from Xero (2026-09-11, the MD's ask: "the portal can't recognise when a
/// sales invoice is paid"). list_xero_sales_invoices is every sales invoice on the project's mapped
/// Xero contact in ANY status — PAID included, which the aged receivables can never show — so the
/// model can find the Xero number for a valuation invoice keyed into Xero by hand (match by net,
/// date and reference the way a person would) and see whether Xero says PAID.
/// preview_valuation_invoice_payment_sync is the invoices section's "Sync payments from Xero…"
/// modal: what sync_valuation_invoice_payments_from_xero would link and record. Nothing here writes.
/// </summary>
internal static partial class AiValuationInvoiceTools
{
    private static IEnumerable<AiTool> XeroSalesInvoiceTools() => new AiTool[]
    {
        new(
            "list_xero_sales_invoices",
            "Every sales invoice Xero holds on the Xero contact MAPPED ON THE PROJECT, in ANY status "
            + "(PAID, AUTHORISED, DRAFT, SUBMITTED, VOIDED — the aged receivables read never shows a "
            + "paid one), newest first, read fresh from Xero: invoiceId, number (INV-0227), reference "
            + "(\"Valuation 05\"), status, date, dueDate, net (SubTotal, ex VAT), vat, gross, "
            + "amountPaid, amountDue and fullyPaidOnDate. portalMatches says, per Xero row, which "
            + "portal valuation invoice (id + VI-nnnn) is already linked to it by Xero id or number. "
            + "This is how you find the Xero invoice number for a valuation invoice that was keyed "
            + "into Xero by hand and carries no number in the portal: compare the portal invoice's "
            + "net Amount (list_valuation_invoices) with each row's net, then the date and the "
            + "reference, the way a person reading both lists would — propose the pairing to the "
            + "user and on their yes record_valuation_invoice_xero_number. Never pick between two "
            + "rows that both fit. It is also how you see whether Xero says an invoice is PAID: "
            + "status PAID with amountDue 0 — the portal records that with "
            + "preview_valuation_invoice_payment_sync / sync_valuation_invoice_payments_from_xero, "
            + "never by asking the user whether it was paid. Pass search to narrow by number or "
            + "reference, status to narrow by Xero status.",
            AiToolSchema.Object(
                ("projectId", "string", "The project whose mapped Xero contact's invoices to read (list_projects returns ids).", true),
                ("search", "string", "Optional text matched against the Xero invoice number and reference (case-insensitive).", false),
                ("status", "string", "Optional Xero status to keep: PAID, AUTHORISED, DRAFT, SUBMITTED, VOIDED.", false)),
            AiToolKind.Read,
            JpmsRoleSets.AllInternal,
            async (context, input, ct) =>
            {
                var projectId = ProjectId(context, input);
                if (string.IsNullOrWhiteSpace(projectId)) return Fail("projectId is required (list_projects returns ids).");
                var project = await context.Db.Projects.AsNoTracking().SingleOrDefaultAsync(row => row.ProjectId == projectId, ct);
                if (project is null) return Fail($"Project {projectId} not found.");
                if (string.IsNullOrWhiteSpace(project.XeroContactId))
                    return Fail($"No Xero contact is mapped on {project.Name} — map it with set_project_xero_contact (list_xero_customers) first.");

                var snapshot = await context.Services.GetRequiredService<IXeroClient>()
                    .GetSalesInvoicesForContactAsync(project.XeroContactId, ct);
                if (!snapshot.IsConfigured) return Fail("Xero is not connected on this portal.");
                if (snapshot.Error is not null) return Fail($"Xero could not be read: {snapshot.Error}");

                var search = AiToolSchema.Text(input, "search")?.Trim();
                var status = AiToolSchema.Text(input, "status")?.Trim();
                var portal = await context.Db.ValuationInvoices.AsNoTracking()
                    .Where(row => row.ProjectId == projectId)
                    .Select(row => new { row.ValuationInvoiceId, row.Reference, row.XeroInvoiceId, row.XeroInvoiceNumber })
                    .ToListAsync(ct);

                var rows = snapshot.Invoices
                    .Where(row => string.IsNullOrEmpty(status) || row.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .Where(row => string.IsNullOrEmpty(search)
                        || (row.Number ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                        || (row.Reference ?? "").Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return Serialise(new
                {
                    ok = true,
                    projectId,
                    projectName = project.Name,
                    xeroContactId = project.XeroContactId,
                    contactName = string.IsNullOrWhiteSpace(project.XeroContactName) ? project.ClientName : project.XeroContactName,
                    fetchedAtUtc = snapshot.FetchedAtUtc,
                    count = rows.Count,
                    invoices = rows.Select(row => new
                    {
                        invoiceId = row.InvoiceId,
                        number = row.Number,
                        reference = row.Reference,
                        status = row.Status,
                        date = row.Date?.ToString("yyyy-MM-dd"),
                        dueDate = row.DueDate?.ToString("yyyy-MM-dd"),
                        net = row.SubTotal,
                        vat = row.TotalTax,
                        gross = row.Total,
                        amountPaid = row.AmountPaid,
                        amountDue = row.AmountDue,
                        fullyPaidOnDate = row.FullyPaidOnDate?.ToString("yyyy-MM-dd"),
                        isPaid = row.IsPaid
                    }),
                    portalMatches = rows
                        .Select(row => new
                        {
                            xeroInvoiceId = row.InvoiceId,
                            number = row.Number,
                            portalInvoice = portal.FirstOrDefault(candidate =>
                                (!string.IsNullOrWhiteSpace(candidate.XeroInvoiceId) && string.Equals(candidate.XeroInvoiceId, row.InvoiceId, StringComparison.OrdinalIgnoreCase))
                                || (!string.IsNullOrWhiteSpace(candidate.XeroInvoiceNumber) && !string.IsNullOrWhiteSpace(row.Number)
                                    && string.Equals(candidate.XeroInvoiceNumber.Trim(), row.Number.Trim(), StringComparison.OrdinalIgnoreCase)))
                        })
                        .Where(match => match.portalInvoice is not null)
                        .Select(match => new
                        {
                            match.xeroInvoiceId,
                            match.number,
                            valuationInvoiceId = match.portalInvoice!.ValuationInvoiceId,
                            reference = match.portalInvoice.Reference
                        }),
                    note = "Match an unlinked portal invoice by net amount first, then date and reference; propose the pairing before record_valuation_invoice_xero_number. "
                        + "A PAID row is recorded in the portal by preview_valuation_invoice_payment_sync then sync_valuation_invoice_payments_from_xero — never ask the user whether it was paid."
                });
            }),

        new(
            "preview_valuation_invoice_payment_sync",
            "What sync_valuation_invoice_payments_from_xero WOULD do for a project, read fresh from "
            + "Xero — the invoices section's \"Sync payments from Xero…\" modal. One row per ISSUED "
            + "valuation invoice: an invoice already linked to Xero (id or number) is followed to its "
            + "Xero row — PAID there plans RecordPayment (the portal's net amount, paidOn = Xero's "
            + "fully-paid date; note flags a net that differs), part paid or unpaid plans nothing and "
            + "says so; an invoice with no Xero link is matched to the contact's Xero invoices the way "
            + "a person would (reference/number naming the valuation — \"VI-0005\", \"Valuation 05\" — "
            + "else the same net to the penny): exactly one match plans Link (+ RecordPayment when it "
            + "is PAID), none plans nothing, two or more is Ambiguous and lists the candidates — the "
            + "sync never guesses. Raised/Submitted/Approved invoices are not yet certified and are "
            + "left alone; Paid and Cancelled are done. blockers names why nothing can run (no Xero "
            + "contact mapped on the project, Xero unreadable). Show the user EVERY row — invoice, "
            + "Xero number, action, amount, paid date, note, the ambiguous candidates — take their "
            + "yes, then sync_valuation_invoice_payments_from_xero with confirm true. An ambiguous "
            + "row is resolved with list_xero_sales_invoices + record_valuation_invoice_xero_number, "
            + "then preview and sync again.",
            AiToolSchema.Object(
                ("projectId", "string", "The project (list_projects returns ids).", true)),
            AiToolKind.Read,
            ValuationInvoiceRoles.AllowedToManageValuationInvoices,
            async (context, input, ct) =>
            {
                var projectId = ProjectId(context, input);
                if (string.IsNullOrWhiteSpace(projectId)) return Fail("projectId is required (list_projects returns ids).");
                try
                {
                    var preview = await context.Services
                        .GetRequiredService<IQueryHandler<PreviewValuationInvoicePaymentSync, ValuationInvoicePaymentSyncPreview>>()
                        .HandleAsync(new PreviewValuationInvoicePaymentSync(projectId), ct);
                    return Serialise(new
                    {
                        ok = true,
                        preview.ProjectId,
                        preview.ProjectName,
                        preview.XeroContactId,
                        preview.XeroContactName,
                        preview.FetchedAtUtc,
                        preview.Blockers,
                        hasWork = preview.HasWork,
                        summary = new { preview.LinksPlanned, preview.PaymentsPlanned, preview.NoChange },
                        rows = preview.Rows.Select(row => new
                        {
                            row.ValuationInvoiceId,
                            row.Reference,
                            status = row.Status.ToString(),
                            row.Amount,
                            row.XeroInvoiceId,
                            row.XeroInvoiceNumber,
                            row.XeroStatus,
                            xeroDate = row.XeroDate?.ToString("yyyy-MM-dd"),
                            action = row.Action.ToString(),
                            row.AmountToRecord,
                            paidOn = row.PaidOn?.ToString("yyyy-MM-dd"),
                            row.Note,
                            row.Candidates
                        }),
                        note = preview.Blockers.Count > 0
                            ? "Blocked — fix the blocker (map the Xero contact with list_xero_customers + set_project_xero_contact) and preview again."
                            : preview.HasWork
                                ? $"Would link {preview.LinksPlanned} and record {preview.PaymentsPlanned} payment(s). Show every row to the user, take their yes, then sync_valuation_invoice_payments_from_xero with confirm true."
                                : "Nothing to record — every Issued invoice is unpaid, part paid, unmatched or ambiguous in Xero; the notes say which. Ambiguous rows are linked by hand with record_valuation_invoice_xero_number."
                    });
                }
                catch (InvalidOperationException refusal)
                {
                    return Fail(refusal.Message);
                }
            })
    };
}
