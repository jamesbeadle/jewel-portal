using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Connect;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The MCP connector's static contracts (docs/ai/10-mcp-connector.md): the tool catalogue is
// role-filtered exactly as the endpoints gate, the write tools exist for the roles that may use
// them, and the hand-kept registries still agree (the boot drift check). These pin the surface a
// team member's AI tool is offered — a regression here is a tool silently vanishing from, or
// leaking into, someone's Claude.
public sealed class AiConnectorTests
{
    private static SignedInUser UserWith(params Role[] roles) =>
        new("test@jewelbb.co.uk", "Test User", roles);

    [Fact]
    public void DriftCheck_passes()
    {
        // Throws on drift — the same call the boot makes.
        AiRegistryDriftCheck.Assert();
    }

    [Fact]
    public void ToolNames_areUnique()
    {
        var names = AiToolCatalogue.All.Select(tool => tool.Name).ToList();
        Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Catalogue_carriesNoUiTools()
    {
        // The chat's browser-executed tools are gone; everything left runs server-side.
        Assert.DoesNotContain(AiToolCatalogue.All, tool => tool.Kind == AiToolKind.Ui);
    }

    [Fact]
    public void ForConnector_filtersByRole()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();

        Assert.Contains("list_projects", director);
        Assert.Contains("get_request_context", director);
        // A subcontractor contact must not be offered the internal read surface.
        Assert.DoesNotContain("list_projects", subcontractor);
        Assert.DoesNotContain("get_valuation_context", subcontractor);
        Assert.True(director.Count > subcontractor.Count);
    }

    [Fact]
    public void WriteTools_exist_andAreMarkedAsWrites()
    {
        var admin = AiToolCatalogue.ForConnector(UserWith(Role.Admin));
        foreach (var name in new[] { "post_request_message", "add_todo", "complete_todo", "log_todo_progress", "save_skill" })
        {
            var tool = admin.SingleOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(tool);
            Assert.Equal(AiToolKind.Write, tool!.Kind);
        }
    }

    [Fact]
    public void SaveSkill_isNotOfferedOutsideTheSkillGate()
    {
        var quantitySurveyor = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name);
        Assert.DoesNotContain("save_skill", quantitySurveyor);
    }

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback", true)]
    [InlineData("https://www.perplexity.ai/rest/connections/oauth_callback", true)]
    [InlineData("http://localhost:33418/callback", true)]
    [InlineData("http://127.0.0.1:8976/oauth/callback", true)]
    [InlineData("http://evil.example/callback", false)]
    [InlineData("not-a-url", false)]
    [InlineData("", false)]
    public void RedirectUris_allowHttpsAndLoopbackOnly(string uri, bool acceptable)
    {
        Assert.Equal(acceptable, OAuthRedirects.IsAcceptable(uri));
    }

    [Fact]
    public void ActionRegistry_buildsAndSelfAsserts()
    {
        // Construction IS the assertion: unique names, real stamp parameters, and a typed
        // Allows/Check overload for every command (the 2026-08-28 review found "first overload"
        // selection breaking 22 actions on shared gate classes — this pins the fix).
        var actions = AiActionRegistry.All;
        Assert.True(actions.Count > 150, $"expected a full surface, got {actions.Count}");
        foreach (var action in actions)
            _ = AiActionSchema.InputSchema(action);
    }

    [Fact]
    public void ActionRegistry_roleFiltersLikeThePortal()
    {
        bool Offered(string name, params Role[] roles) =>
            AiActionRegistry.All.Single(a => a.Name == name).VisibleTo.IncludesAny(roles);

        Assert.True(Offered("approve_variation_order", Role.ManagingDirector));
        Assert.False(Offered("approve_variation_order", Role.Subcontractor));
        Assert.False(Offered("delete_project", Role.QuantitySurveyor));
        Assert.False(Offered("issue_valuation_invoice", Role.Subcontractor));
    }

    [Fact]
    public void ActionGateway_toolsExistForEveryRole()
    {
        foreach (var role in System.Enum.GetValues<Role>())
        {
            var names = AiToolCatalogue.ForConnector(UserWith(role)).Select(t => t.Name).ToList();
            Assert.Contains("list_actions", names);
            Assert.Contains("describe_action", names);
            Assert.Contains("perform_action", names);
        }
    }

    [Fact]
    public void ParityAuditReadTools_exist_andStayInternal()
    {
        // The 2026-08-31 read surface (docs/ai/11 §5): the register a mirrored write acts on must
        // be readable by the roles that write it, and none of it may leak to a subcontractor.
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();

        var readTools = new[]
        {
            "list_valuation_invoices", "list_valuation_snapshots", "get_valuation_snapshot",
            "list_triage_queue", "get_mailbox_message", "list_mailbox_conversation",
            "search_mailbox", "list_document_triage", "list_project_communications",
            "get_weekly_cashflow_plan", "get_aged_payables", "get_aged_receivables",
            "list_payment_certificates", "list_xero_ledger_lines",
            "list_leads", "list_rates", "list_tender_enquiries", "list_clients",
            "list_architects", "list_workers", "list_company_registers", "list_portal_users",
            "list_rfis_across_projects", "list_useful_information",
            "list_calendar_events", "get_building_control", "get_programme",
            "list_architect_instructions", "list_progress",
            "get_package_reconciliation"
        };
        foreach (var name in readTools)
        {
            Assert.Contains(name, director);
            Assert.DoesNotContain(name, subcontractor);
        }

        // list_drawings deliberately mirrors JpmsRoleSets.DrawingReaders, which ADMITS
        // subcontractors — they read revisions for their assigned work, exactly as over HTTP.
        Assert.Contains("list_drawings", director);
        Assert.Contains("list_drawings", subcontractor);
    }

    [Fact]
    public void ParityAuditActions_areDeclared()
    {
        // The write gaps recorded in docs/ai/11 §2 and the §4 unlocks, now declared.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "create_weekly_cashflow_item", "update_weekly_cashflow_item",
            "archive_weekly_cashflow_item", "place_weekly_cashflow_entry",
            "set_weekly_cashflow_exclusion", "save_weekly_cashflow_supplier_group",
            "remove_weekly_cashflow_supplier_group",
            "add_inventory_item", "update_inventory_item", "create_inventory_item_from_message",
            "import_architect_instruction_from_message", "update_architect_instruction",
            "link_architect_instruction_to_variation", "unlink_architect_instruction_from_variation",
            "delete_architect_instruction",
            "create_request_from_message", "discard_mailbox_message",
            "restore_mailbox_message", "remove_mailbox_message_tag",
            "attach_action_skills"
        })
        {
            Assert.Contains(name, names);
        }

        // Deleting an instruction says "permanently" — the registry's own boot assert requires
        // the confirm-first flag, pinned here too so a rewording never drops the gate.
        Assert.True(AiActionRegistry.All.Single(a => a.Name == "delete_architect_instruction").RequiresConfirmation);
    }

    [Fact]
    public void SaveSkillReference_isAWriteToolBehindTheSkillGate()
    {
        var admin = AiToolCatalogue.ForConnector(UserWith(Role.Admin));
        var tool = admin.SingleOrDefault(candidate => candidate.Name == "save_skill_reference");
        Assert.NotNull(tool);
        Assert.Equal(AiToolKind.Write, tool!.Kind);
        Assert.DoesNotContain("save_skill_reference",
            AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name));
    }
}
