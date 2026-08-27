using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The assistant is held together by several hand-kept registries that must agree: the tool
/// catalogue, ModalCatalog, the open_modal tool's own description of the dialogs, and the status
/// labels the panel shows. They have drifted before — select_email shipped half-wired, page
/// guides lagged new dialogs, labels missed the slowest tools — and every drift surfaces as the
/// assistant confidently narrating something that never happened.
///
/// <para>This check runs once at registration and THROWS on drift. That is deliberate: the
/// registries are static data compiled into this assembly, so the check is deterministic — it
/// cannot fail intermittently, and it cannot pass locally then fail deployed. Failing the boot is
/// the point: a drifted registry never reaches a user.</para>
/// </summary>
public static class AiRegistryDriftCheck
{
    public static void Assert()
    {
        var complaints = new List<string>();

        var openModal = AiToolCatalogue.All.FirstOrDefault(tool =>
            string.Equals(tool.Name, "open_modal", StringComparison.OrdinalIgnoreCase));
        if (openModal is null) complaints.Add("open_modal is not in the tool catalogue.");

        // Page-anchored dialogs are deliberately NOT openable via open_modal — the page supplies
        // their anchor (tender_reply's tender email) when it starts the task itself.
        var pageAnchored = new[] { ModalCatalog.TenderReply.ModalKey, ModalCatalog.TenderEnquiryAnswers.ModalKey };

        foreach (var modal in ModalCatalog.All)
        {
            if (pageAnchored.Contains(modal.ModalKey, StringComparer.OrdinalIgnoreCase)) continue;

            if (openModal is not null
                && !openModal.Description.Contains($"\"{modal.ModalKey}\"", StringComparison.Ordinal))
            {
                complaints.Add(
                    $"open_modal's description never mentions \"{modal.ModalKey}\" — the model is "
                    + "never told the dialog exists. Describe it there (and in the modal_key "
                    + "enum) in the same commit that registers a dialog.");
            }

            if (AiToolLabels.For("open_modal", $"{{\"modal_key\":\"{modal.ModalKey}\"}}") == "Opening a dialog")
            {
                complaints.Add(
                    $"AiToolLabels has no open_modal line for \"{modal.ModalKey}\" — the user "
                    + "watches the generic \"Opening a dialog\" instead of what is happening.");
            }
        }

        foreach (var tool in AiToolCatalogue.All)
        {
            if (AiToolLabels.For(tool.Name, null) == "Working on it")
            {
                complaints.Add(
                    $"AiToolLabels has no label for \"{tool.Name}\" — the user watches "
                    + "\"Working on it\" for its whole run, and the slow tools run longest.");
            }
        }

        // The evidence rule must name every source-reading tool, and every tool it names must
        // exist — a reader the prompt never mentions is a reader the model never reaches for, and
        // a name the prompt mentions that the catalogue lacks is a model promising a call it
        // cannot make.
        foreach (var name in AiSourceTools.Names)
        {
            if (!AiSystemPrompt.EvidenceRule.Contains(name, StringComparison.Ordinal))
                complaints.Add($"AiSystemPrompt.EvidenceRule never mentions \"{name}\" — the model is never told to use it.");
            if (!AiToolCatalogue.All.Any(tool => string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase)))
                complaints.Add($"AiSourceTools.Names lists \"{name}\" but the tool catalogue has no such tool.");
        }

        // Every record type must be REACHABLE from chat — a deterministic tool path from what a
        // user says to a record id — or carry a written, deliberate exemption. This is the check
        // that turns "the assistant can't see X" from a user-discovered failure into a build
        // failure: adding a RecordType without deciding its chat reach no longer compiles past boot.
        // (2026-08-27: a draft work order was unreachable by every path — no number until approval,
        // no list tool — and the gap was found by a user asking to edit one.)
        foreach (var recordType in Enum.GetValues<RecordType>())
        {
            if (!ChatReachability.TryGetValue(recordType, out var reach))
            {
                complaints.Add(
                    $"RecordType.{recordType} has no entry in AiRegistryDriftCheck.ChatReachability — "
                    + "decide how chat reaches it (which tools) or write down why it is exempt, in the "
                    + "same commit that adds the record type.");
                continue;
            }

            foreach (var toolName in reach.Tools)
            {
                if (!AiToolCatalogue.All.Any(tool => string.Equals(tool.Name, toolName, StringComparison.OrdinalIgnoreCase)))
                {
                    complaints.Add(
                        $"ChatReachability says RecordType.{recordType} is reached by \"{toolName}\" "
                        + "but the tool catalogue has no such tool.");
                }
            }
        }

        if (complaints.Count > 0)
        {
            throw new InvalidOperationException(
                "The AI registries have drifted out of step:\n- " + string.Join("\n- ", complaints));
        }
    }

    /// <summary>How chat reaches each record type: the tools that resolve a user's words to an id,
    /// or the written reason there are none. Hand-kept on purpose — the point is that a human
    /// decides, and the boot check above refuses a record type nobody decided about.</summary>
    private sealed record RecordReach(string[] Tools, string? Exemption = null)
    {
        public static RecordReach Via(params string[] tools) => new(tools);
        public static RecordReach None(string why) => new(Array.Empty<string>(), why);
    }

    private static readonly IReadOnlyDictionary<RecordType, RecordReach> ChatReachability =
        new Dictionary<RecordType, RecordReach>
        {
            [RecordType.Request] = RecordReach.Via("list_requests", "get_request_context", "find_by_reference"),
            [RecordType.BidPackageInvite] = RecordReach.Via("list_bid_packages", "get_bid_package_context", "find_by_reference"),
            [RecordType.CostCentre] = RecordReach.Via("list_cost_codes", "get_valuation_context"),
            [RecordType.Todo] = RecordReach.Via("list_todos", "find_by_reference"),
            [RecordType.Variation] = RecordReach.Via("list_variations", "get_variation_context", "find_by_reference"),
            // Unified into VariationOrders 2026-07-23 — the variation tools are the quote's tools.
            [RecordType.VariationQuote] = RecordReach.Via("list_variations", "get_variation_context", "find_by_reference"),
            [RecordType.WorkOrder] = RecordReach.Via("list_work_orders", "get_work_order_context", "find_by_reference"),
            [RecordType.Defect] = RecordReach.Via("list_defects", "find_by_reference"),
            [RecordType.ValuationReportSnapshot] = RecordReach.Via("get_valuation_context"),
            [RecordType.TenderEnquiry] = RecordReach.Via("get_tender_enquiry_context"),

            // Record-less tag families: correspondence buckets, not records — there is no id for a
            // list tool to return. read_record_emails reads them when a page supplies the scope.
            [RecordType.SubcontractorComms] = RecordReach.None("record-less tag family — no ids to list"),
            [RecordType.SupplierComms] = RecordReach.None("record-less tag family — no ids to list"),
            [RecordType.InternalComms] = RecordReach.None("record-less tag family — no ids to list"),

            // DECLARED GAPS — chat cannot reach these yet. Each stays a one-line entry here until
            // its tool ships; deleting the line without shipping the tool fails the boot.
            [RecordType.Scheduling] = RecordReach.None("GAP: no chat tool reads the programme yet"),
            [RecordType.Lad] = RecordReach.None("GAP: no chat tool lists LAD claims yet"),
            [RecordType.CalendarEvent] = RecordReach.None("GAP: calendar shipped 2026-08-27, chat tool pending"),
            [RecordType.BuildingControlCase] = RecordReach.None("GAP: building control shipped 2026-08-27, chat tool pending"),
            [RecordType.BuildingControlInspection] = RecordReach.None("GAP: building control shipped 2026-08-27, chat tool pending"),
        };

}
