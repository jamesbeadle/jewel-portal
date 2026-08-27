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
            "list_work_orders" => Named(arguments, "search", search => $"Finding work orders: {search}")
                                  ?? "Reading the work orders",
            "navigate_to" => "Taking you there",
            "switch_agent" => Named(arguments, "agent", agent => $"Bringing in the {agent} agent")
                              ?? "Changing agent",
            "load_skill" => Named(arguments, "skill_key", key => $"Reading the {key} skill")
                            ?? "Reading a skill",
            "load_skill_reference" => Named(arguments, "ref_key", key => $"Reading {key}")
                                      ?? "Reading a reference",
            "get_request_context" => "Reading the request's working papers",
            "get_bid_package_context" => "Reading the bid package",
            "get_work_order_context" => Named(arguments, "reference", reference => $"Reading {reference}")
                                        ?? "Reading the work order",
            "get_variation_context" => Named(arguments, "reference", reference => $"Reading {reference}")
                                       ?? "Reading the variation",
            "get_valuation_context" => "Reading the valuation report",
            "list_request_correspondence" => "Reading the correspondence",
            "list_cost_codes" => "Reading the cost codes",
            "load_page_guide" => Named(arguments, "route", route => $"Reading the guide for {route}")
                                 ?? "Reading the page guide",
            "read_record_emails" => "Reading the tagged emails",
            "read_selected_email" => "Reading the open email",
            "read_email_attachment" => "Reading an attachment",
            "list_sources" => "Looking at what files are to hand",
            "find_in_source" => Named(arguments, "query", query => $"Searching the files for {query}")
                                ?? "Searching the files",
            "read_source" => Named(arguments, "part", part => $"Reading {part}")
                             ?? "Reading a file",
            "get_tender_enquiry_context" => "Reading the tender enquiry",
            "read_tender_enquiry_document" => "Reading the enquiry's document",
            "select_email" => Named(arguments, "search", search => $"Opening the email: {search}")
                              ?? "Opening an email",
            "stage_triage_tag" => Named(arguments, "reference", reference => $"Staging the {reference} tag")
                                  ?? "Staging the tag",
            "stage_triage_todo" => Named(arguments, "title", title => $"Staging to-do: {title}")
                                   ?? "Staging a to-do",
            "stage_triage_work_order" => Named(arguments, "title", title => $"Staging work order: {title}")
                                         ?? "Staging a work order",
            "open_modal" => Named(arguments, "modal_key", key => key switch
                            {
                                "compose_email" => "Opening the email composer",
                                "reply_email" => "Opening the reply",
                                "manual_variation" => "Opening the Add-variation form",
                                "variation_draft" => "Opening the variation draft",
                                "work_order_edit" => "Opening the work order for editing",
                                "work_order_create" => "Opening the Add work order form",
                                "bid_package_details" => "Opening the package details",
                                "manual_timesheet" => "Opening the Add-a-day form",
                                "record_absence" => "Opening the Record-absence form",
                                "worker_week" => "Opening the week entry",
                                "variation_edit_lines" => "Opening the variation's lines for editing",
                                "claim_progress" => "Opening the % complete entry",
                                "variation_build_up" => "Opening the agreed build-up",
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
