using System.Text.Json;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The valuation-invoice register and the frozen report snapshots, readable (2026-08-31). The
/// parity audit's deepest blind-write pair (docs/ai/11 §3): all nine invoice lifecycle actions
/// were mirrored while the register itself — statuses, certified-to-date, what the client was
/// actually sent — was invisible. Each tool wraps the SAME query handler its endpoint composes.
/// </summary>
internal static class AiValuationInvoiceTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static string? ProjectId(AiToolContext context, JsonElement input) =>
        AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "list_valuation_invoices",
                "A project's valuation-invoice register — every VI with its status (Raised, "
                + "Submitted, Approved, Issued, Paid, Cancelled), amount, deposit credit, gross "
                + "certificate, payment received and lifecycle dates — plus the project's money "
                + "summary: total raised, awaiting approval, invoiced, certified to date, paid and "
                + "outstanding. Call this BEFORE any valuation-invoice action: it is the register "
                + "those actions change.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids).", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var invoices = await context.Services
                        .GetRequiredService<IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>>()
                        .HandleAsync(new ListValuationInvoicesForProject(projectId), ct);
                    var summary = await context.Services
                        .GetRequiredService<IQueryHandler<GetProjectValuationInvoiceSummary, ProjectValuationInvoiceSummary>>()
                        .HandleAsync(new GetProjectValuationInvoiceSummary(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        summary = new
                        {
                            summary.TotalRaised,
                            summary.TotalAwaitingApproval,
                            summary.TotalInvoiced,
                            summary.TotalDepositCredited,
                            certifiedToDate = summary.TotalCertified,
                            summary.TotalPaid,
                            summary.Outstanding
                        },
                        invoices = invoices.Select(invoice => new
                        {
                            invoice.ValuationInvoiceId,
                            number = invoice.DisplayNumber,
                            periodMonth = invoice.PeriodMonth.ToString("yyyy-MM"),
                            status = invoice.Status.ToString(),
                            invoice.Amount,
                            invoice.DepositCredited,
                            grossCertificate = invoice.CertifiedAmount,
                            invoice.AmountPaid,
                            invoice.RaisedAt,
                            invoice.SubmittedAt,
                            invoice.ApprovedAt,
                            invoice.IssuedAt,
                            invoice.PaidAt,
                            invoice.RejectedAt,
                            invoice.RejectionReason,
                            invoice.AmendmentCount,
                            invoice.IsManual,
                            snapshotId = invoice.ValuationReportSnapshotId
                        }),
                        note = "The client-facing statement behind an invoice is its FROZEN snapshot "
                               + "(get_valuation_snapshot), never the live report."
                    });
                }),

            new(
                "list_valuation_snapshots",
                "A project's frozen valuation-report snapshots — each the exact statement a client "
                + "was (or could be) sent: label, when taken, the invoice it backs, whether a later "
                + "snapshot supersedes it, and its frozen summary figures. The live report is a "
                + "working copy; a snapshot is the issued record.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var projectId = ProjectId(context, input);
                    if (string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids).");

                    var snapshots = await context.Services
                        .GetRequiredService<IQueryHandler<ListValuationReportSnapshotsForProject, IReadOnlyList<ValuationReportSnapshot>>>()
                        .HandleAsync(new ListValuationReportSnapshotsForProject(projectId), ct);

                    return Serialise(new
                    {
                        ok = true,
                        projectId,
                        count = snapshots.Count,
                        snapshots = snapshots.Select(snapshot => new
                        {
                            snapshot.ValuationReportSnapshotId,
                            snapshot.Label,
                            snapshot.TakenAt,
                            invoiceId = snapshot.ValuationInvoiceId,
                            snapshot.IsSuperseded,
                            snapshot.ContractSum,
                            snapshot.NetVariations,
                            snapshot.RevisedContractSum,
                            snapshot.TotalWorksComplete,
                            snapshot.RetentionPercent,
                            snapshot.RetentionHeld
                        }),
                        note = "get_valuation_snapshot(valuationReportSnapshotId) returns a snapshot's frozen lines."
                    });
                }),

            new(
                "get_valuation_snapshot",
                "One frozen valuation-report snapshot in full — the summary footer and every "
                + "frozen line (section, variation ref, cost code, description, quantity, rate) "
                + "exactly as the statement stood when it was taken. This is what the client saw; "
                + "compare against get_valuation_context for what has moved since.",
                AiToolSchema.Object(
                    ("valuationReportSnapshotId", "string", "The snapshot's id from list_valuation_snapshots or list_valuation_invoices.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var snapshotId = AiToolSchema.Text(input, "valuationReportSnapshotId")?.Trim();
                    if (string.IsNullOrWhiteSpace(snapshotId))
                        return Fail("A valuationReportSnapshotId is required — list_valuation_snapshots returns them.");

                    ValuationReportSnapshotDetail detail;
                    try
                    {
                        detail = await context.Services
                            .GetRequiredService<IQueryHandler<GetValuationReportSnapshot, ValuationReportSnapshotDetail>>()
                            .HandleAsync(new GetValuationReportSnapshot(snapshotId), ct);
                    }
                    catch (InvalidOperationException)
                    {
                        return Fail($"No snapshot exists with id \"{snapshotId}\" — "
                                    + "list_valuation_snapshots returns the ids that do.");
                    }

                    return Serialise(new
                    {
                        ok = true,
                        snapshot = detail.Snapshot,
                        lines = detail.Lines.Select(line => new
                        {
                            line.SectionCode,
                            line.SectionName,
                            line.VariationRef,
                            lineType = line.LineType.ToString(),
                            line.CostCode,
                            clientReference = line.ClientReference,
                            line.Description,
                            line.Unit,
                            line.Quantity,
                            line.Rate,
                            line.LineAmount,
                            line.PercentComplete,
                            line.CumulativeClaimed,
                            line.PeriodIncrement,
                            countsTowardTotals = line.CountsTowardTotals
                        })
                    });
                })
        };
    }
}
