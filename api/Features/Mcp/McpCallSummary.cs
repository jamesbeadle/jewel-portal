using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;

namespace Jewel.JPMS.Api.Features.Mcp;

/// <summary>
/// What the agent activity log keeps of one connector call: the tool and enough of its arguments to
/// reconstruct what was asked — except an action on the onboarding forms, whose arguments are a
/// person's details (a share code, a passport number, a leaving date). Those are kept on the forms'
/// own records under their own readers, and the log names the action alone.
/// </summary>
internal static class McpCallSummary
{
    private const int LongestArguments = 600;
    private const string ActionTool = "perform_action";

    public static string Of(string toolName, JsonElement arguments)
    {
        var actionName = toolName == ActionTool ? AiToolSchema.Text(arguments, "name") : null;
        var isAFormsAction = actionName is not null && AiActionRegistry.Find(actionName)?.Area == FormsActions.Area;
        if (isAFormsAction) return $"MCP tool call {toolName} {actionName} (a form's details are not logged)";
        var raw = arguments.GetRawText();
        var shown = raw.Length > LongestArguments ? raw[..LongestArguments] + "…" : raw;
        return $"MCP tool call {toolName} {shown}";
    }
}
