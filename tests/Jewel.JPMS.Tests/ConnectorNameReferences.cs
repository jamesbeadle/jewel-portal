using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;

namespace Jewel.JPMS.Tests;

/// <summary>
/// A snake_case word that reads like a tool or an action is a PROMISE, wherever the team writes
/// one — a tool description, an action's notes or input schema, or a skill the portal stores.
/// When the name resolves to nothing the model tells the user the portal cannot do the thing, and
/// it is right from where it sits. This is the one reading of "does that name resolve", so the
/// catalogue guard and the skill guard can never disagree about what counts as a name.
/// </summary>
internal static class ConnectorNameReferences
{
    private static readonly HashSet<string> Verbs = new(StringComparer.Ordinal)
    {
        "list", "get", "find", "read", "view", "search", "add", "create", "update", "set", "link",
        "unlink", "import", "send", "record", "delete", "approve", "undo", "push", "preview",
        "export", "load", "save", "run", "post", "complete", "log", "suggest", "rename",
        "consolidate", "promote", "raise", "issue", "mark", "code", "prepare", "describe",
        "perform", "reject", "assign", "close", "reopen", "archive", "clear", "move", "cancel",
        "stage", "remove", "apply", "allocate", "recode", "attach", "register", "schedule",
        "chase", "extract", "submit", "confirm", "withdraw", "draft", "file", "tag", "invite",
        "decline", "accept", "award", "revise", "settle", "void", "restore", "upload", "enable",
        "query", "rebuild", "disable", "pin", "unpin", "flag", "unflag", "request", "plan", "book"
    };

    /// <summary>Suffixes that make a snake_case word an argument rather than a door.</summary>
    private static readonly string[] ArgumentEndings = { "_id", "_type", "_key" };

    private static readonly Regex SnakeCaseName = new(
        @"(?<![A-Za-z0-9_/.\-])([a-z][a-z0-9]*(?:_[a-z0-9]+)+)(?![A-Za-z0-9_/\-])",
        RegexOptions.Compiled);

    /// <summary>Every name in one piece of text that reads like a door and opens onto nothing,
    /// each already written as the line a failing test prints.</summary>
    internal static IEnumerable<string> DanglingIn(string where, string? text) =>
        SnakeCaseName.Matches(text ?? "")
            .Select(match => match.Groups[1].Value)
            .Where(ReadsLikeADoor)
            .Where(DoesNotResolve)
            .Select(name => $"{where} names '{name}'");

    private static bool ReadsLikeADoor(string name) =>
        Verbs.Contains(name.Split('_')[0])
        && !ArgumentEndings.Any(ending => name.EndsWith(ending, StringComparison.Ordinal));

    private static bool DoesNotResolve(string name) =>
        AiToolCatalogue.Find(name) is null && AiActionRegistry.Find(name) is null;
}
