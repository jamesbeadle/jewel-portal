using Jewel.JPMS.Api.Features.Ai.Tools;

namespace Jewel.JPMS.Api.Features.Ai;

/// <summary>
/// The connector's tool catalogue is hand-kept static data, and it has drifted before — a record
/// type nobody wired a tool for surfaces as "the assistant can't see X", found by a user instead
/// of the build. This check runs once at registration and THROWS on drift. That is deliberate:
/// the registries are compiled into this assembly, so the check is deterministic — it cannot fail
/// intermittently, and it cannot pass locally then fail deployed. Failing the boot is the point.
///
/// <para>Slimmed 2026-08-27 with the retirement of the in-portal chat: the dialog (ModalCatalog),
/// status-label and system-prompt checks went with the machinery they checked. What remains is the
/// reachability rule, which applies to the MCP connector exactly as it did to chat.</para>
/// </summary>
public static class AiRegistryDriftCheck
{
    public static void Assert()
    {
        var complaints = new List<string>();

        // The action gateway's registry asserts itself (unique names, real stamps, resolvable
        // gates) lazily on first build; force it HERE so a mis-declared action fails the boot,
        // never a user's first perform_action call.
        try
        {
            _ = Tools.Actions.AiActionRegistry.All;
        }
        catch (InvalidOperationException ex)
        {
            complaints.Add(ex.Message);
        }

        // Duplicate tool names would make tools/call ambiguous.
        var duplicates = AiToolCatalogue.All
            .GroupBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        foreach (var name in duplicates)
            complaints.Add($"The tool catalogue registers \"{name}\" more than once.");

        // Every source-reading tool named by AiSourceTools must exist in the catalogue.
        foreach (var name in AiSourceTools.Names)
        {
            if (!AiToolCatalogue.All.Any(tool => string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase)))
                complaints.Add($"AiSourceTools.Names lists \"{name}\" but the tool catalogue has no such tool.");
        }

        // Every record type must be REACHABLE from the connector — a deterministic tool path from
        // what a user says to a record id — or carry a written, deliberate exemption. Adding a
        // RecordType without deciding its reach no longer compiles past boot.
        foreach (var recordType in Enum.GetValues<RecordType>())
        {
            if (!Reachability.TryGetValue(recordType, out var reach))
            {
                complaints.Add(
                    $"RecordType.{recordType} has no entry in AiRegistryDriftCheck.Reachability — "
                    + "decide how the connector reaches it (which tools) or write down why it is exempt, "
                    + "in the same commit that adds the record type.");
                continue;
            }

            foreach (var toolName in reach.Tools)
            {
                if (!AiToolCatalogue.All.Any(tool => string.Equals(tool.Name, toolName, StringComparison.OrdinalIgnoreCase)))
                {
                    complaints.Add(
                        $"Reachability says RecordType.{recordType} is reached by \"{toolName}\" "
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

    /// <summary>How the connector reaches each record type: the tools that resolve a user's words
    /// to an id, or the written reason there are none. Hand-kept on purpose — the point is that a
    /// human decides, and the boot check above refuses a record type nobody decided about.</summary>
    private sealed record RecordReach(string[] Tools, string? Exemption = null)
    {
        public static RecordReach Via(params string[] tools) => new(tools);
        public static RecordReach None(string why) => new(Array.Empty<string>(), why);
    }

    private static readonly IReadOnlyDictionary<RecordType, RecordReach> Reachability =
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
            // The claims list (ids, names, statuses) comes back with the report.
            [RecordType.ValuationClaim] = RecordReach.Via("get_valuation_context"),

            // Record-less tag families: correspondence buckets, not records — there is no id for a
            // list tool to return. read_record_emails reads them when given the scope.
            [RecordType.SubcontractorComms] = RecordReach.None("record-less tag family — no ids to list"),
            [RecordType.SupplierComms] = RecordReach.None("record-less tag family — no ids to list"),
            [RecordType.InternalComms] = RecordReach.None("record-less tag family — no ids to list"),

            // DECLARED GAPS — the connector cannot reach these yet. Each stays a one-line entry
            // here until its tool ships; deleting the line without shipping the tool fails the boot.
            [RecordType.Scheduling] = RecordReach.None("GAP: no tool reads the programme yet"),
            [RecordType.Lad] = RecordReach.None("GAP: no tool lists LAD claims yet"),
            [RecordType.CalendarEvent] = RecordReach.None("GAP: calendar shipped 2026-08-27, connector tool pending"),
            [RecordType.BuildingControlCase] = RecordReach.None("GAP: building control shipped 2026-08-27, connector tool pending"),
            [RecordType.BuildingControlInspection] = RecordReach.None("GAP: building control shipped 2026-08-27, connector tool pending"),
            [RecordType.Inventory] = RecordReach.None("GAP: inventory shipped 2026-08-28, connector tool pending"),
            [RecordType.SiteInstruction] = RecordReach.None("GAP: site instructions shipped 2026-09-03, connector tool pending"),
            [RecordType.TenderEnquiry] = RecordReach.None("GAP: get_tender_enquiry_context was removed in 45a6ebf (2026-09-03); the record type stays (persisted as 13, routed by TriageCategories) so it needs a replacement tool"),
        };
}
