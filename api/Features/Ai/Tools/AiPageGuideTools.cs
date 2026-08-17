using System.Text.Json;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The tool that turns the site map's one-line notes into working knowledge. The map (pinned in
/// the system prompt) says a page exists; load_page_guide returns that page's full guide from
/// <see cref="PageGuideCatalogue"/> — the manual workflow, the assistant's verbs there, and what
/// is deliberately done elsewhere. On demand for the same reason unpinned skills are: sixty
/// guides pinned into every prompt would drown the turn.
/// </summary>
internal static class AiPageGuideTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "load_page_guide",
                "Load the working guide for one portal page: what it is, what a person does on it "
                + "(tabs, buttons, dialogs, workflows), what YOU can do there, and what is "
                + "deliberately done elsewhere. Pass a route from the site map — the template "
                + "(\"/projects/{project}/requests\") or a real route (\"/projects/abc12/requests\") "
                + "both work. Call it for the page you are about to work — the OPEN page first — "
                + "whenever you have not read that page's guide this conversation, and before "
                + "telling the user where an action lives on a page you have not read.",
                AiToolSchema.Object(("route", "string", "A route from the site map, template or real.", true)),
                AiToolKind.Read,
                JpmsRoleSets.AllInternal,
                (context, input, ct) =>
                {
                    var route = AiToolSchema.Text(input, "route")?.Trim();
                    if (string.IsNullOrWhiteSpace(route))
                        return Task.FromResult(Fail("A route is required — use one from the site map."));

                    var guide = PageGuideCatalogue.FindForRoute(route);
                    if (guide is null)
                        return Task.FromResult(Fail(
                            $"No guide matches {route}. Use a route exactly as the site map spells "
                            + "it; record detail routes need their full template."));

                    return Task.FromResult(JsonSerializer.Serialize(new
                    {
                        ok = true,
                        route = guide.RouteTemplate,
                        name = guide.DisplayName,
                        guide = guide.Guide
                    }, Json));
                })
        };
    }

    private static string Fail(string message) =>
        JsonSerializer.Serialize(new { ok = false, error = message }, Json);
}
