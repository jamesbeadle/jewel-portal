using Jewel.JPMS.Api.Features.Manual.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The site manual over the connector (2026-09-29): the office master, one module in full, and a
/// published role view. Each tool wraps the SAME query handler its HTTP endpoint composes and
/// mirrors that endpoint's role gate exactly. The writes are perform_action's manual actions.
/// </summary>
internal static class AiManualTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build() => new AiTool[] { ListManualModules(), GetManualModule(), GetManualView() };

    private static AiTool ListManualModules() => new(
        "list_manual_modules",
        "The site manual's office master — every module (GOV-01 governance, RUN-01 daily routine, "
        + "WFL-01 variation workflow and so on) with its status (Draft, InReview, Approved, Superseded), "
        + "working version, published version, owner, approver, next review date, the role views it is "
        + "published to and how many have acknowledged the published version. The text itself is "
        + "get_manual_module. The manual actions (create_manual_module, update_manual_module_draft, "
        + "submit_manual_module_for_review, approve_manual_module …) act on these ids.",
        AiToolSchema.Empty(),
        AiToolKind.Read,
        ManualRoles.AllowedToReadMaster,
        async (context, _, ct) =>
        {
            var modules = await context.Services
                .GetRequiredService<IQueryHandler<ListManualModules, IReadOnlyList<ManualModule>>>()
                .HandleAsync(new ListManualModules(), ct);
            return Serialise(new { ok = true, count = modules.Count, modules = modules.Select(Summary) });
        });

    private static AiTool GetManualModule() => new(
        "get_manual_module",
        "One module of the site manual in full: its working text and controls, the text the site "
        + "currently sees (the last approved version), every approved version and every acknowledgement.",
        AiToolSchema.Object(("manual_module_id", "string", "The module's id, as list_manual_modules gives it.", true)),
        AiToolKind.Read,
        ManualRoles.AllowedToReadMaster,
        async (context, input, ct) =>
        {
            var manualModuleId = AiToolSchema.Text(input, "manual_module_id")?.Trim();
            if (string.IsNullOrWhiteSpace(manualModuleId)) return Fail("A manual_module_id is required.");
            var detail = await context.Services
                .GetRequiredService<IQueryHandler<GetManualModule, ManualModuleDetail?>>()
                .HandleAsync(new GetManualModule(manualModuleId), ct);
            if (detail is null) return Fail("No module has that id — list_manual_modules shows the ids that exist.");
            var module = detail.Module;
            return Serialise(new { ok = true, module = Summary(module), module.Body, detail.PublishedBody, detail.Versions, detail.Acknowledgements });
        });

    private static AiTool GetManualView() => new(
        "get_manual_view",
        "A published view of the site manual — SiteManager, HealthAndSafetyOfficer, Foreman or Office — "
        + "as its readers see it: only approved, unretired modules published to that view, with the "
        + "approved text and whether the signed-in user has acknowledged each version. Drafts never appear.",
        AiToolSchema.Object(("view", "string", "SiteManager, HealthAndSafetyOfficer, Foreman or Office.", true)),
        AiToolKind.Read,
        ManualRoles.AllowedToReadViews,
        async (context, input, ct) =>
        {
            var asked = AiToolSchema.Text(input, "view")?.Trim() ?? "";
            if (!Enum.TryParse<ManualView>(asked, ignoreCase: true, out var view)) return Fail($"There is no view called {asked}.");
            var reader = context.User;
            var reading = await context.Services.GetRequiredService<GetManualViewHandler>()
                .HandleAsync(new GetManualView(view), reader.Email, ct);
            return Serialise(new { ok = true, reading.View, reading.IssuedAt, reading.OutstandingCount, reading.Modules });
        });

    private static object Summary(ManualModule module) => new
    {
        module.ManualModuleId, module.Code, module.Title, module.Purpose, module.Family,
        status = module.Status.ToString(), module.Version, module.PublishedVersion,
        module.OwnerEmail, module.ApproverEmail, module.ApprovedAt, module.NextReviewAt,
        module.Audience, module.LinkedFormSlugs, module.LinkedStandards, module.ChangeSummary,
        module.SourceSections, module.AcknowledgedCount
    };
}
