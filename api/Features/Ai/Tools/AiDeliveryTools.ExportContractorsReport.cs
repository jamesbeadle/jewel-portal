using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// export_contractors_report (Jeremy, 24 Sep 2026: the report could only leave the portal by a
/// person pressing Download): the SAME build the page's Download PDF runs — composed, gated by the
/// wording findings, rendered — handed over as an expiring download link, because a report of
/// fifty photographs is ten megabytes and a tool call carries words. The link is the same kind
/// export_valuation_report and large email attachments travel by. Every export is audited.
/// </summary>
internal static partial class AiDeliveryTools
{
    private static AiTool ExportContractorsReport()
    {
        return new(
            "export_contractors_report",
            "The Contractor's Report as the PDF the report page's Download PDF produces, built from "
            + "the register now and handed over as a download link that works for seven days — "
            + "fetch it to bring the file into the chat, or give the link to the user. Refused, with "
            + "the wording findings by section and line, while findings stand (the same refusal as "
            + "the page); fix them and export again. The PDF is the issued document — there is no "
            + "Word copy. The portal never emails the report: a person sends it to the client.",
            AiToolSchema.Object(
                ("contractorsReportId", "string", "From list_contractors_reports.", true)),
            AiToolKind.Read,
            ProgressRoles.Readers,
            ExportContractorsReportAsync);
    }

    private static async Task<string> ExportContractorsReportAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var reportId = AiToolSchema.Text(input, "contractorsReportId");
        if (string.IsNullOrWhiteSpace(reportId)) return Fail("contractorsReportId is required (list_contractors_reports returns ids).");
        var shareStore = context.Services.GetRequiredService<IEmailFileShareStore>();
        if (!shareStore.IsConfigured)
            return Fail("This portal host cannot mint download links. The PDF is one click away on the report's page: Download PDF.");

        var outcome = await context.Services.GetRequiredService<ContractorsReportBuilder>().BuildAsync(reportId, ct);
        if (outcome is null) return Fail($"Contractor's Report {reportId} not found.");
        if (outcome.IsRefused) return Serialise(new { ok = false, refused = true, findings = outcome.Findings });

        var file = outcome.File!;
        var link = await shareStore.ShareAsync("contractors-reports", file.FileName, file.ContentType, file.Content, ct);
        if (link is null) return Fail("A download link could not be signed. The PDF is one click away on the report's page: Download PDF.");

        await context.Services.GetRequiredService<AuditTrail>().WriteAsync(
            AuditEventType.ContractorsReportExported, $"{file.FileName} exported as a download link",
            recordId: reportId, recordReference: file.FileName, actorEmail: context.User.Email, cancellationToken: ct);
        return Serialise(new
        {
            ok = true,
            fileName = link.FileName,
            sizeBytes = link.SizeBytes,
            url = link.Url.ToString(),
            expiresAt = link.ExpiresAt
        });
    }
}
