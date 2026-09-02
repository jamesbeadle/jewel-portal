using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Contracts.BuildingControl;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiDeliveryTools
{
    private static AiTool GetBuildingControl()
    {
        return new(
            "get_building_control",
            "A project's building control in one answer: the case(s) with the body — regime "
            + "(local authority or private registered approver), the body's reference, contact, "
            + "case status and official dates — plus the inspection register (BCI refs; status "
            + "ladder Planned → Booked → Inspected → Passed / Actions required → Closed, where "
            + "Actions required re-books the SAME record) and every file on the case or its "
            + "inspections. Cases come newest-first, the active one leading.",
            AiToolSchema.Object(
                ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
            AiToolKind.Read,
            BuildingControlRoles.Readers,
            GetBuildingControlAsync);
    }

    private static async Task<string> GetBuildingControlAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);

        var view = await Query<GetBuildingControlForProject, BuildingControlProjectView>(
            context, new GetBuildingControlForProject(projectId), ct);
        return Serialise(new
        {
            ok = true,
            projectId,
            cases = view.Cases.Select(CaseRow),
            inspections = view.Inspections.Select(InspectionRow),
            attachments = view.Attachments.Select(AttachmentRow)
        });
    }

    private static object CaseRow(BuildingControlCase item) => new
    {
        item.BuildingControlCaseId,
        item.Reference,
        regime = item.Regime.ToString(),
        item.BodyName,
        item.BodyReference,
        item.ContactName,
        item.ContactEmail,
        status = item.Status.ToString(),
        item.NoticeSubmittedOn,
        item.AcceptedOn,
        item.CompletionCertifiedOn,
        item.Notes
    };

    private static object InspectionRow(BuildingControlInspection item) => new
    {
        item.BuildingControlInspectionId,
        caseId = item.BuildingControlCaseId,
        item.Reference,
        item.StageName,
        status = item.Status.ToString(),
        item.BookedFor,
        item.InspectedAt,
        item.OutcomeNotes,
        item.InspectorName
    };

    private static object AttachmentRow(BuildingControlAttachment item) => new
    {
        item.BuildingControlAttachmentId,
        caseId = item.BuildingControlCaseId,
        inspectionId = item.BuildingControlInspectionId,
        kind = item.Kind.ToString(),
        item.FileName,
        item.AddedAt
    };
}
