using System.Text.Json;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// Turns a tool call into the line a person sees beside the pulsing jewel.
///
/// <para>Present continuous, specific where the arguments allow it — "Looking up V72" beats
/// "Searching". Kept out of <see cref="AiToolCatalogue"/> deliberately: the catalogue's descriptions
/// are written for the model, these are written for the user, and the two should be free to diverge
/// without one being edited by accident while changing the other.</para>
/// </summary>
public static class AiToolLabels
{
    public static string For(string toolName, string? argumentsJson)
    {
        var arguments = Parse(argumentsJson);

        return toolName switch
        {
            "get_current_context" => "Checking where you are",
            "list_projects" => "Looking through the projects",
            "get_project_contract" => "Reading the contract",
            "list_variations" => Named(arguments, "status", status => $"Finding variations that are {Humanise(status)}")
                                 ?? "Reading the variations",
            "list_requests" => Named(arguments, "kind", kind => $"Reading the {kind} register")
                               ?? "Reading the requests",
            "find_by_reference" => Named(arguments, "reference", reference => $"Looking up {reference}")
                                   ?? "Looking that up",
            "navigate_to" => "Taking you there",
            "switch_agent" => Named(arguments, "agent", agent => $"Bringing in the {agent} agent")
                              ?? "Changing agent",
            "load_skill" => Named(arguments, "skill_key", key => $"Reading the {key} skill")
                            ?? "Reading a skill",
            "load_skill_reference" => Named(arguments, "ref_key", key => $"Reading {key}")
                                      ?? "Reading a reference",
            "read_record_emails" => "Reading the tagged emails",
            "read_email_attachment" => "Reading an attachment",
            "open_modal" => Named(arguments, "modal_key", key => key switch
                            {
                                "compose_email" => "Opening the email composer",
                                "manual_variation" => "Opening the Add-variation form",
                                "variation_draft" => "Opening the variation draft",
                                _ => "Opening a dialog"
                            }) ?? "Opening a dialog",
            "update_open_modal" => "Writing into the form",
            _ => "Working on it"
        };
    }

    /// <summary>The enum names the model uses are not what a person says out loud.</summary>
    private static string Humanise(string status) => status switch
    {
        "AwaitingArchitectInstruction" => "awaiting an AI",
        "Quoting" => "still in quoting",
        "Issued" => "issued",
        "Approved" => "approved",
        "Rejected" => "rejected",
        _ => status.ToLowerInvariant()
    };

    private static string? Named(JsonElement? arguments, string property, Func<string, string> format)
    {
        // A designation cannot be declared inside a `not` pattern (CS8780) — test, then take.
        if (arguments is not { ValueKind: JsonValueKind.Object }) return null;
        var element = arguments.Value;
        if (!element.TryGetProperty(property, out var value)) return null;
        if (value.ValueKind != JsonValueKind.String) return null;

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : format(text.Trim());
    }

    private static JsonElement? Parse(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson)) return null;
        try
        {
            // Clone: the JsonDocument is disposed on the way out and the element must outlive it.
            using var document = JsonDocument.Parse(argumentsJson);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
