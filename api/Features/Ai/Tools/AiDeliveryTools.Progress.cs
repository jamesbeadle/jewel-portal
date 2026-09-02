using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool ListProgress()
    {
        return new(
            "list_progress",
            "A project's progress registers in one answer: the updates (a site manager's dated "
            + "record of works — title, description, work date, weather, photo count) and the "
            + "client-facing reports (title, period, narrative sections, and WHICH updates each "
            + "includes). Reports are assembled FROM existing updates — an update's photos "
            + "illustrate every report that selects it — and a report's PDF regenerates from "
            + "this register on every download, so it always reflects the register as it stands.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
            AiToolKind.Read,
            ProgressRoles.Readers,
            ListProgressAsync);
    }

    private static async Task<string> ListProgressAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var updates = await Query<ListProgressUpdatesForProject, IReadOnlyList<ProgressUpdate>>(
            context, new ListProgressUpdatesForProject(projectId), ct);
        var reports = await Query<ListProgressReportsForProject, IReadOnlyList<ProgressReport>>(
            context, new ListProgressReportsForProject(projectId), ct);
        return Serialise(new
        {
            ok = true,
            projectId,
            updates = updates.Select(UpdateRow),
            reports = reports.Select(ReportRow)
        });
    }

    private static object UpdateRow(ProgressUpdate update) => new
    {
        update.ProgressUpdateId,
        update.Title,
        update.Description,
        update.WorkDate,
        weather = update.Weather?.Summary,
        photoCount = update.Photos.Count,
        update.CreatedByEmail,
        update.CreatedAt
    };

    private static object ReportRow(ProgressReport report) => new
    {
        report.ProgressReportId,
        report.Title,
        report.PeriodStart,
        report.PeriodEnd,
        report.Introduction,
        report.WorkCompleted,
        report.UpcomingWorks,
        report.CreatedByEmail,
        report.CreatedAt,
        includedUpdateIds = report.SelectedUpdateIds
    };
}
