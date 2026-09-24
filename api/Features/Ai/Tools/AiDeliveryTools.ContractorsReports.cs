using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool ListContractorsReports()
    {
        return new(
            "list_contractors_reports",
            "A project's weekly Contractor's Reports (the nine-section PLG report), newest period "
            + "first: number, the Friday-to-Thursday period, Valuation No., programme reference, "
            + "prepared by / issued to, date of issue, the Look Ahead items, and which progress "
            + "updates are selected. Only the ENTERED fields live on the record — Sections 1, 3, "
            + "4, 7, 8 and 9 are read from the register when the report is built; get_contractors_report "
            + "shows the composed document.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
            AiToolKind.Read,
            ProgressRoles.Readers,
            ListContractorsReportsAsync);
    }

    private static async Task<string> ListContractorsReportsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);
        var reports = await Query<ListContractorsReports, IReadOnlyList<ContractorsReport>>(context, new ListContractorsReports(projectId), ct);
        return Serialise(new { ok = true, projectId, reports = reports.Select(ContractorsReportRow) });
    }

    private static AiTool GetContractorsReport()
    {
        return new(
            "get_contractors_report",
            "One Contractor's Report with the document it composes to RIGHT NOW — the header, "
            + "Section 1's days with the selected updates, Look Ahead, the open RFIs (Section 3), "
            + "the variations not yet approved or rejected with their total (Section 4), "
            + "Neighbours, Health & Safety, the Building Control contact and liaison (Section 7), "
            + "Section 8 (only the work orders with attendance entered — a firm that did not "
            + "attend is not in the report, so its wording is never checked), the days with photographs "
            + "(Section 9) — plus the updates in the period to choose from, workOrdersOnSite[] "
            + "(every order on site, with its workOrderId, reference and target completion, to "
            + "enter attendance against with update_contractors_report — the reference and target "
            + "are for you to match on and are NEVER written into the report: name the "
            + "subcontractor and the work, no WO number, no target date) and the wording "
            + "FINDINGS. While findings is non-empty the Word and PDF builds are refused: the "
            + "report goes to the client's side, so a line naming remedial works, making good, "
            + "rectification, snagging, defects or rework is refused by section and line, never "
            + "reworded silently. Fix the line on the record (update_contractors_report for the "
            + "entered text, update_progress_update for a site note) and read again.",
            AiToolSchema.Object(
                ("contractorsReportId", "string", "From list_contractors_reports.", true)),
            AiToolKind.Read,
            ProgressRoles.Readers,
            GetContractorsReportAsync);
    }

    private static async Task<string> GetContractorsReportAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var reportId = AiToolSchema.Text(input, "contractorsReportId");
        if (string.IsNullOrWhiteSpace(reportId)) return Fail("contractorsReportId is required (list_contractors_reports returns ids).");
        var view = await Query<GetContractorsReport, ContractorsReportView?>(context, new GetContractorsReport(reportId), ct);
        if (view is null) return Fail($"Contractor's Report {reportId} not found.");
        return Serialise(new
        {
            ok = true,
            report = ContractorsReportRow(view.Report),
            document = view.Document,
            canBeBuilt = view.Document.CanBeBuilt,
            findings = view.Document.Findings,
            updatesInPeriod = view.UpdatesInPeriod,
            workOrdersOnSite = view.WorkOrdersOnSite,
            downloads = new
            {
                pdf = $"contractors-reports/{view.Report.ContractorsReportId}/pdf",
                word = $"contractors-reports/{view.Report.ContractorsReportId}/docx"
            }
        });
    }

    private static object ContractorsReportRow(ContractorsReport report) => new
    {
        report.ContractorsReportId,
        report.ProjectId,
        report.Number,
        title = report.DisplayTitle,
        report.PeriodStart,
        report.PeriodEnd,
        report.ValuationNumber,
        report.ProgrammeReference,
        report.PreparedByName,
        report.IssuedTo,
        report.DateOfIssue,
        report.LookAhead,
        report.Neighbours,
        report.HealthAndSafety,
        report.BuildingControlLiaison,
        report.Attendance,
        report.SelectedUpdateIds,
        report.CreatedByEmail,
        report.CreatedAt,
        report.UpdatedAt
    };
}
