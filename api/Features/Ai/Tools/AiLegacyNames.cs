namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// Old connector names that still resolve. The project register was renamed Drawings →
/// Documents on 2026-09-03 (it holds party-wall awards, building-control letters and reports as
/// well as drawings), and every tool and action name followed the register. Saved skills,
/// doctrine attached on the AI Actions admin page, and the team's own habits still say
/// list_drawings / register_drawing — so a lookup by an old name lands on the renamed entry, and
/// the catalogue (list_actions, tools/list) only ever advertises the new one. Nothing here is
/// exposed to the model; it is a courtesy at the lookup, never a second name.
/// </summary>
internal static class AiLegacyNames
{
    private static readonly IReadOnlyDictionary<string, string> Renamed =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Read tool
            ["list_drawings"] = "list_documents",
            // Actions — Documents area
            ["register_drawing"] = "register_document",
            ["update_drawing_metadata"] = "update_document_metadata",
            ["approve_drawing_revision"] = "approve_document_revision",
            ["set_drawing_revision_label"] = "set_document_revision_label",
            ["delete_drawing_revision"] = "delete_document_revision",
            ["delete_drawing"] = "delete_document",
            ["create_drawing_folder"] = "create_document_folder",
            ["rename_drawing_folder"] = "rename_document_folder",
            ["delete_drawing_folder"] = "delete_document_folder",
            ["move_drawing_to_folder"] = "move_document_to_folder",
            // Actions elsewhere
            ["file_document_as_drawing"] = "file_document_to_project_documents",
            ["set_bid_package_drawings"] = "set_bid_package_documents",
            // Action AREA (what skills can be attached to on the admin page)
            ["Drawings"] = "Documents"
        };

    /// <summary>The current name for <paramref name="name"/> — itself when it was never renamed.</summary>
    public static string Current(string name) =>
        Renamed.TryGetValue(name, out var current) ? current : name;

    /// <summary>Every name a current entry has ever had, the current one first — for matching rows
    /// (attached skills) that were written under the old spelling.</summary>
    public static IReadOnlyList<string> AllNamesFor(string current)
    {
        var names = new List<string> { current };
        foreach (var pair in Renamed)
            if (string.Equals(pair.Value, current, StringComparison.OrdinalIgnoreCase)) names.Add(pair.Key);
        return names;
    }
}
