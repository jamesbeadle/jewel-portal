using Jewel.JPMS.Api.Features.Hs.Audits;
using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

// The H&S reads (2026-09-15): the project's site audits, one audit's form, and the register the
// connector could write to but never read. Each mirrors its endpoint's gate exactly.
internal static partial class AiDeliveryTools
{
    /// <summary>Mirror of ListHsRecordsEndpoint.RolesThatMayReadHsRecords.</summary>
    private static readonly RoleSet HsRecordReaders = JpmsRoleSets.AllInternal;

    private static AiTool ListHsAudits() => new(
        "list_hs_audits",
        "A project's H&S site audits (HSA refs), newest first: inspection date, type, safety "
        + "officer, site manager, the score as her sheet computes it (rate average less a penalty "
        + "per class present, once each) with its rating band "
        + "(Poor / Fair / Good / Very good), the previous audit's score, and status Draft → Issued "
        + "→ Closed. Issue mints the corrective actions; get_hs_audit reads the items.",
        AiToolSchema.Object(
            ("projectId", "string", "Defaults to the project in view; pass it otherwise.", false)),
        AiToolKind.Read,
        HsAuditRoles.Readers,
        ListHsAuditsAsync);

    private static AiTool GetHsAudit() => new(
        "get_hs_audit",
        "One H&S site audit with every item of its framework in order — 11 sections, 165 items "
        + "on the 2026-09-15 framework (182 on 2026-08-27) — each with the officer's comment code "
        + "(N/A, N, N/C, N/S, R), rate (0 / 5 / 10), class (A–E), time-scale (I, 1, 3, 7, 1M, O), "
        + "findings, owner and date rectified, plus "
        + "the corrective action id Issue minted for it. Every item carries the hsAuditItemId that "
        + "update_hs_audit_items takes.",
        AiToolSchema.Object(
            ("hsAuditId", "string", "From list_hs_audits.", true)),
        AiToolKind.Read,
        HsAuditRoles.Readers,
        GetHsAuditAsync);

    private static AiTool ListHsRecords() => new(
        "list_hs_records",
        "The H&S register: every observation, near miss, incident, corrective action, toolbox "
        + "talk and permit across projects, newest first — kind, summary, severity, status (Open / "
        + "InProgress / Closed), the assignee (a portal email or a person's name), raised, due and "
        + "closed dates. Filter by projectId and/or kind. Corrective actions minted by an audit "
        + "carry the audit item's code in their summary.",
        AiToolSchema.Object(
            ("projectId", "string", "Narrow to one project (defaults to the project in view when one is; pass \"all\" for every project).", false),
            ("kind", "string", "Narrow to one kind: Observation, NearMiss, Incident, CorrectiveAction, ToolboxTalk or Permit.", false),
            ("openOnly", "boolean", "true lists only Open and InProgress records.", false)),
        AiToolKind.Read,
        HsRecordReaders,
        ListHsRecordsAsync);

    private static async Task<string> ListHsAuditsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        if (string.IsNullOrWhiteSpace(projectId)) return Fail(NoProject);
        var audits = await Query<ListHsAuditsForProject, IReadOnlyList<HsAudit>>(context, new ListHsAuditsForProject(projectId), ct);
        return Serialise(new { ok = true, projectId, audits = audits.Select(AuditRow) });
    }

    private static async Task<string> GetHsAuditAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var hsAuditId = AiToolSchema.Text(input, "hsAuditId");
        if (string.IsNullOrWhiteSpace(hsAuditId)) return Fail("Pass hsAuditId (list_hs_audits returns ids).");
        var view = await Query<GetHsAudit, HsAuditView>(context, new GetHsAudit(hsAuditId), ct);
        return Serialise(new
        {
            ok = true,
            audit = AuditRow(view.Audit),
            sections = HsAuditTemplate.Sections.Select(section => new { section.Number, section.Name }),
            items = view.Items.Select(AuditItemRow)
        });
    }

    private static async Task<string> ListHsRecordsAsync(AiToolContext context, JsonElement input, CancellationToken ct)
    {
        var projectId = ProjectId(context, input);
        var kindText = AiToolSchema.Text(input, "kind");
        HsRecordKind? kind = Enum.TryParse<HsRecordKind>(kindText, true, out var parsedKind) ? parsedKind : null;
        if (!string.IsNullOrWhiteSpace(kindText) && kind is null) return Fail($"Unknown kind '{kindText}'.");
        var openOnly = AiToolSchema.Flag(input, "openOnly") ?? false;

        var records = await Query<ListHsRecords, IReadOnlyList<HsRecord>>(context, new ListHsRecords(), ct);
        var narrowed = records
            .Where(record => projectId is null or "all" || string.Equals(record.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .Where(record => kind is null || record.Kind == kind)
            .Where(record => !openOnly || record.Status != HsStatus.Closed);
        return Serialise(new { ok = true, records = narrowed.Select(RecordRow) });
    }

    private static object AuditRow(HsAudit audit) => new
    {
        audit.HsAuditId,
        audit.Reference,
        audit.ProjectId,
        status = audit.Status.ToString(),
        type = audit.Type.ToString(),
        audit.InspectionDate,
        audit.SafetyOfficerName,
        audit.SiteManagerName,
        audit.Score,
        scorePercent = audit.Score is { } score ? HsAuditScoring.PercentText(score) : null,
        rating = audit.Rating?.ToString(),
        audit.PreviousScore,
        audit.SummaryOfWorkActivities,
        audit.SiteOperativeCount,
        audit.FurtherComments,
        audit.TemplateVersion,
        audit.ManagerName,
        audit.IssuedAt,
        audit.ClosedAt
    };

    private static object AuditItemRow(HsAuditItem item) => new
    {
        item.HsAuditItemId,
        item.Code,
        item.Section,
        item.Name,
        comment = item.Comment?.Code(),
        rate = item.Rate is { } rate ? (int)rate : (int?)null,
        @class = item.Class?.Letter(),
        item.Minus,
        timeScale = item.TimeScale?.Code(),
        item.Findings,
        item.OwnerName,
        item.DateRectified,
        item.HsRecordId
    };

    private static object RecordRow(HsRecord record) => new
    {
        record.HsRecordId,
        record.ProjectId,
        kind = record.Kind.ToString(),
        record.Summary,
        severity = record.Severity.ToString(),
        status = record.Status.ToString(),
        record.AssignedToEmail,
        record.AssignedToName,
        record.RaisedAt,
        record.DueAt,
        record.ClosedAt
    };
}
