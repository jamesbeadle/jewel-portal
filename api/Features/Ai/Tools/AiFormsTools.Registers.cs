using Jewel.JPMS.Api.Features.Forms;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiFormsTools
{
    private static AiTool RightToWorkTool() => new(
        "list_right_to_work_checks",
        "The right to work register: rightToWorkCheckId and every field of each check but the share code or IDSP reference "
        + "(hasAReference says one is on file; the code itself is read on the page), whether it is cleared (a pass with all "
        + "three confirmations and a named checker), what is still missing (gaps), a late check, the evidence on file and the "
        + "day the person was emailed the confirmation. save_right_to_work_check corrects one; send_right_to_work_confirmation "
        + "emails a cleared one. Right-to-work readers only.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.RightToWorkReaders,
        RightToWorkAsync);

    private static async Task<string> RightToWorkAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var checks = await Query<ListRightToWorkChecks, IReadOnlyList<RightToWorkCheck>>(context, new ListRightToWorkChecks(), ct);
        return Serialise(new { ok = true, checks = checks.Select(CheckRow) });
    }

    private static object CheckRow(RightToWorkCheck check)
    {
        var details = check.Details;
        return new
        {
            check.RightToWorkCheckId, details = details with { Reference = "" }, hasAReference = details.Reference.Length > 0,
            check.IsCleared, check.IsLate, gaps = RightToWorkRules.GapsIn(details),
            check.EvidenceUploadId, check.EvidenceFileName, check.RecordedByEmail, check.RecordedAt,
            check.EngagementEndedOn, check.ConfirmedOn
        };
    }

    private static AiTool TrainingTool() => new(
        "list_training_records",
        "The training register: trainingRecordId, the person, course, provider, certificate number, completed and expiry dates, "
        + "the standing today (Valid, ExpiringSoon, Expired, NoExpiry, Ended), and when the renewal was last asked for. "
        + "set_training_record_details corrects one; accept_training_certificate adds one from a form.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.Office,
        TrainingAsync);

    private static async Task<string> TrainingAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var records = await Query<ListTrainingRecords, IReadOnlyList<TrainingRecord>>(context, new ListTrainingRecords(), ct);
        var today = FormClock.Today();
        return Serialise(new { ok = true, records = records.Select(record => new { record, standing = record.StandingOn(today).ToString() }) });
    }

    private static AiTool WorkstationsTool() => new(
        "list_workstation_actions",
        "Every NO from every workstation assessment — the queue of things to sort out, open first: workstationActionId, the "
        + "person, the workstation, what to sort out, its state (Open, Fixed, Accepted) and the note. resolve_workstation_action "
        + "closes one.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        FormRoleSets.Office,
        WorkstationsAsync);

    private static async Task<string> WorkstationsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var actions = await Query<ListWorkstationActions, IReadOnlyList<WorkstationAction>>(context, new ListWorkstationActions(), ct);
        return Serialise(new { ok = true, actions });
    }
}
