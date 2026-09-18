using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The valuation-invoice register and the valuations with their statements, readable (2026-08-31;
/// snapshots consolidated into the claim 2026-09-18 — list_valuations / get_valuation_statement). The
/// parity audit's deepest blind-write pair (docs/ai/11 §3): all nine invoice lifecycle actions
/// were mirrored while the register itself — statuses, certified-to-date, what the client was
/// actually sent — was invisible. Each tool wraps the SAME query handler its endpoint composes.
/// export_valuation_report (2026-09-02, the accountant's ask) adds the report as FILES: the
/// portal's own PDF and workbook, rendered by the download endpoints' builders and handed over
/// as expiring links — so an AI session pulls the real document instead of rebuilding one.
/// </summary>
internal static partial class AiValuationInvoiceTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    private static string? ProjectId(AiToolContext context, JsonElement input) =>
        AiToolSchema.Text(input, "projectId") ?? context.Scope?.ProjectId;

    public static IReadOnlyList<AiTool> Build() => Registers().Concat(XeroRaiseTools()).Concat(XeroSalesInvoiceTools()).ToList();

    private static IReadOnlyList<AiTool> Registers()
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
                            claimId = invoice.ValuationClaimId,
                            invoice.XeroInvoiceId,
                            invoice.XeroInvoiceNumber,
                            invoice.XeroRaisedAt
                        }),
                        note = "The client-facing statement behind an invoice is the LOCKED claim it names "
                               + "(claimId → get_valuation_statement), never the live report."
                    });
                }),

            new(
                "list_valuations",
                "A project's valuations, newest first — one row per period (2026-09-18: the claim is "
                + "the ONE valuation object: tagged, reported on, emailed and invoiced from; there is "
                + "no separate snapshot). Each row carries the claim's id, number, name, date, "
                + "persisted status (Draft / Issued=locked / Confirmed), its derived STAGE (Draft, "
                + "Locked, Invoiced, Sent to client, Approved, Rejected, Certified, Paid, Confirmed), "
                + "when its statement was locked, the live invoice against it, and its summary figures "
                + "(frozen once locked; a Draft's are the live working copy). The ValuationClaimId is "
                + "also the correspondence record id: file_email_to_record (type ValuationClaim) and "
                + "read_record_emails (recordType valuation_claim) work on it. "
                + "get_valuation_statement(valuationClaimId) returns a valuation's statement lines.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                ListValuationsAsync),

            new(
                "list_valuation_snapshots",
                "RETIRED NAME (2026-09-18) — the same answer as list_valuations. The snapshot object "
                + "was consolidated into the valuation claim; call list_valuations.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                ListValuationsAsync),

            new(
                "get_valuation_statement",
                "One valuation's STATEMENT in full — the summary footer and every line (section, "
                + "variation ref, cost code, client reference, description, quantity, rate, amount, "
                + "% complete, claimed to date, this period). For a LOCKED valuation these are its "
                + "own frozen rows — exactly what the client was sent (the PDF, the workbook and the "
                + "emailed statement print the same rows); for a Draft they are the working copy "
                + "computed from the live bill, and the answer says so. Compare a locked statement "
                + "against get_valuation_context for what has moved since.",
                AiToolSchema.Object(
                    ("valuationClaimId", "string", "The valuation's id from list_valuations, get_valuation_context or list_valuation_invoices (claimId). A retired snapshot id is accepted and resolves to its claim.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                (context, input, ct) => GetStatementAsync(context, input, "valuationClaimId", ct)),

            new(
                "get_valuation_snapshot",
                "RETIRED NAME (2026-09-18) — the same answer as get_valuation_statement; a snapshot id "
                + "resolves to the valuation it was frozen from. Call get_valuation_statement.",
                AiToolSchema.Object(
                    ("valuationReportSnapshotId", "string", "A valuation claim id, or a retired snapshot id.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                (context, input, ct) => GetStatementAsync(context, input, "valuationReportSnapshotId", ct)),

            new(
                "export_valuation_report",
                "The portal's OWN valuation report files — the branded PDF and the Excel workbook "
                + "the valuation page's Download / Export buttons produce — rendered server-side "
                + "and handed over as time-limited download links (the same expiring links large "
                + "email attachments travel by). Use this to give the user the report as a file: "
                + "never rebuild a statement from get_valuation_context or get_valuation_statement "
                + "figures when this can hand them the real document. By default it exports the "
                + "project's LATEST valuation — a locked one as its frozen statement, a Draft as the "
                + "working copy stamped as such throughout (the review-before-you-claim export); pass "
                + "valuationClaimId to export a particular valuation. Only a locked valuation's "
                + "statement may be sent to a client. Returns "
                + "one link per file with its name, size and expiry, plus the statement's headline "
                + "figures so you can describe what the file says.",
                AiToolSchema.Object(
                    ("projectId", "string", "Defaults to the project in view; pass it otherwise (list_projects returns ids). Ignored when valuationClaimId is given.", false),
                    ("valuationClaimId", "string", "Export this valuation (list_valuations / get_valuation_context / list_valuation_invoices return ids) instead of the latest one. A retired snapshot id is accepted.", false),
                    ("files", "string", "Which files to render: \"both\" (default), \"pdf\" or \"excel\".", false)),
                AiToolKind.Read,
                // Mirrors DownloadValuationReportDraftPdfEndpoint / DownloadValuationStatementPdfEndpoint:
                // commercial reads are internal-only, external portal logins have no view of project money.
                JpmsRoleSets.AllInternal,
                async (context, input, ct) =>
                {
                    var snapshotId = (AiToolSchema.Text(input, "valuationClaimId") ?? AiToolSchema.Text(input, "valuationReportSnapshotId"))?.Trim();
                    var projectId = ProjectId(context, input);
                    var files = (AiToolSchema.Text(input, "files") ?? "both").Trim().ToLowerInvariant();
                    if (files is not ("both" or "pdf" or "excel"))
                        return Fail("files must be \"both\", \"pdf\" or \"excel\".");
                    if (string.IsNullOrWhiteSpace(snapshotId) && string.IsNullOrWhiteSpace(projectId))
                        return Fail("Say which project: pass projectId (list_projects returns ids), or a valuationClaimId for one valuation.");

                    var shareStore = context.Services.GetRequiredService<IEmailFileShareStore>();
                    if (!shareStore.IsConfigured)
                    {
                        return Fail("This portal host has no file-share store configured, so download links cannot "
                                    + "be minted here. The same files are one click away in the portal: the "
                                    + "valuation page's Download PDF and Export to Excel buttons"
                                    + (string.IsNullOrWhiteSpace(projectId) ? "." : $" (/projects/{projectId}/valuation)."));
                    }

                    var pdfBuilder = context.Services.GetRequiredService<ValuationStatementPdfBuilder>();
                    ValuationReportStatement statement;
                    try
                    {
                        statement = string.IsNullOrWhiteSpace(snapshotId)
                            ? await pdfBuilder.LoadDraftAsync(projectId!, ct)
                            : await pdfBuilder.LoadAsync(snapshotId, ct);
                    }
                    catch (InvalidOperationException)
                    {
                        return Fail(string.IsNullOrWhiteSpace(snapshotId)
                            ? $"No project exists with id \"{projectId}\" — list_projects returns the ids that do."
                            : $"No valuation exists with id \"{snapshotId}\" — list_valuations returns the ids that do.");
                    }

                    var rendered = new List<(string Kind, string FileName, string ContentType, byte[] Content)>();
                    if (files is "both" or "pdf")
                    {
                        var pdf = ValuationStatementPdfBuilder.Render(statement);
                        rendered.Add(("pdf", pdf.FileName, "application/pdf", pdf.Content));
                    }
                    if (files is "both" or "excel")
                    {
                        var workbook = await context.Services
                            .GetRequiredService<ValuationReportWorkbookBuilder>()
                            .BuildAsync(statement, ct);
                        rendered.Add(("excel", workbook.FileName,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", workbook.Content));
                    }

                    // One link per file, or none: a half-shared export would hand the user a PDF
                    // that says one thing and no workbook to reconcile it against.
                    var links = new List<object>();
                    foreach (var (kind, fileName, contentType, content) in rendered)
                    {
                        var link = await shareStore.ShareAsync(statement.ProjectReference, fileName, contentType, content, ct);
                        if (link is null)
                            return Fail("The file-share store could not mint a download link (its credential cannot "
                                        + "sign links). The files are still one click away on the valuation page's "
                                        + "Download PDF and Export to Excel buttons.");
                        links.Add(new
                        {
                            kind,
                            fileName = link.FileName,
                            sizeBytes = link.SizeBytes,
                            url = link.Url.ToString(),
                            expiresAt = link.ExpiresAt
                        });
                    }

                    var snapshot = statement.Statement.Claim;
                    return Serialise(new
                    {
                        ok = true,
                        projectId = statement.ProjectId,
                        projectReference = statement.ProjectReference,
                        projectName = statement.ProjectName,
                        statement = new
                        {
                            label = statement.Statement.Label,
                            isWorkingCopy = statement.IsDraft,
                            valuationClaimId = snapshot.ValuationClaimId,
                            producedAt = statement.Statement.AsAt,
                            lines = statement.Statement.Lines.Count
                        },
                        files = links,
                        figures = new
                        {
                            snapshot.ContractSum,
                            snapshot.NetVariations,
                            snapshot.RevisedContractSum,
                            snapshot.TotalWorksComplete,
                            snapshot.RetentionHeld,
                            snapshot.RetentionReleased,
                            snapshot.CertifiedToDate,
                            snapshot.PaymentDueExVat
                        },
                        note = (statement.IsDraft
                                   ? "This is the LIVE report as a working copy — stamped as such on every page — for "
                                     + "checking a claim before it goes anywhere. A client is only ever sent a locked "
                                     + "valuation's statement: lock the claim first, then export it. "
                                   : "This is the frozen statement exactly as it stood when the valuation was locked. ")
                               + $"The links expire after {AzureBlobEmailFileShareStore.LinkLifetime.TotalDays:0} days; "
                               + "give them to the user as links to click — the files are theirs to download, not "
                               + "content to reproduce."
                    });
                })
        };
    }

    // The one answer behind list_valuations and its retired name list_valuation_snapshots.
    private static async Task<string> ListValuationsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId))
            return Fail("Say which project: pass projectId (list_projects returns ids).");

        var claims = await context.Services
            .GetRequiredService<IQueryHandler<ListValuationClaimsForProject, IReadOnlyList<ValuationClaim>>>()
            .HandleAsync(new ListValuationClaimsForProject(projectId), ct);
        var invoices = await context.Services
            .GetRequiredService<IQueryHandler<ListValuationInvoicesForProject, IReadOnlyList<ValuationInvoice>>>()
            .HandleAsync(new ListValuationInvoicesForProject(projectId), ct);

        return Serialise(new
        {
            ok = true,
            projectId,
            count = claims.Count,
            valuations = claims.OrderByDescending(claim => claim.ClaimNumber).Select(claim =>
            {
                var invoice = ValuationStages.InvoiceFor(claim, invoices);
                return new
                {
                    claim.ValuationClaimId,
                    number = claim.ClaimNumber,
                    name = claim.DisplayName,
                    date = claim.ClaimDate,
                    status = claim.Status.DisplayName(),
                    stage = ValuationStages.Of(claim, invoice).ToString(),
                    lockedAt = claim.LockedAt,
                    claim.ConfirmedAt,
                    invoice = invoice is null ? null : new { invoice.ValuationInvoiceId, number = invoice.DisplayNumber, status = invoice.Status.ToString(), invoice.Amount },
                    claim.ContractSum,
                    claim.NetVariations,
                    claim.RevisedContractSum,
                    claim.TotalWorksComplete,
                    claim.RetentionPercent,
                    claim.RetentionHeld,
                    claim.CertifiedToDate,
                    claim.PaymentDueExVat
                };
            }),
            note = "A locked valuation's figures are frozen; a Draft's stored figures are zero here — "
                   + "get_valuation_context or get_valuation_statement compute its working copy. "
                   + "get_valuation_statement(valuationClaimId) returns a valuation's statement lines."
        });
    }

    // The one answer behind get_valuation_statement and its retired name get_valuation_snapshot.
    private static async Task<string> GetStatementAsync(AiToolContext context, JsonElement input, string idField, CancellationToken ct)
    {
        var id = AiToolSchema.Text(input, idField)?.Trim();
        if (string.IsNullOrWhiteSpace(id))
            return Fail("A valuationClaimId is required — list_valuations returns them.");

        ValuationStatement statement;
        try
        {
            statement = await context.Services
                .GetRequiredService<IQueryHandler<GetValuationStatement, ValuationStatement>>()
                .HandleAsync(new GetValuationStatement(id), ct);
        }
        catch (InvalidOperationException)
        {
            return Fail($"No valuation exists with id \"{id}\" — list_valuations returns the ids that do.");
        }

        var claim = statement.Claim;
        return Serialise(new
        {
            ok = true,
            valuation = new
            {
                claim.ValuationClaimId,
                claim.ProjectId,
                number = claim.ClaimNumber,
                name = claim.DisplayName,
                label = statement.Label,
                status = claim.Status.DisplayName(),
                isWorkingCopy = statement.IsDraft,
                asAt = statement.AsAt,
                claim.ContractSum,
                claim.NetVariations,
                claim.RevisedContractSum,
                claim.TotalWorksComplete,
                claim.RetentionPercent,
                claim.RetentionHeld,
                claim.RetentionReleasePercent,
                claim.RetentionReleased,
                claim.DepositPercent,
                claim.DepositReleased,
                claim.CertifiedToDate,
                claim.PaymentDueExVat
            },
            lines = statement.Lines.Select(line => new
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
            }),
            note = statement.IsDraft
                ? "This valuation is a DRAFT: these lines are the working copy computed from the live bill right now, not a record the client was sent."
                : "This valuation is locked: these are its frozen statement lines — what the client was (or can be) sent."
        });
    }
}
