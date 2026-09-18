using System.Text.Json;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Api.Features.Ai.Tools.Actions;
using Jewel.JPMS.Api.Features.Ai.Tools;
using Jewel.JPMS.Api.Features.Connect;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;
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
        // Probed as the MD, not Admin: Role.Admin is deliberately NOT a member of
        // JpmsRoleSets.AllInternal / InternalAndArchitect (it is the system role, not a delivery
        // role), so the request/todo write tools are not offered to a bare Admin — the Director
        // sits in every one of these gates.
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        foreach (var name in new[] { "post_request_message", "add_todo", "complete_todo", "log_todo_progress", "save_skill" })
        {
            var tool = director.SingleOrDefault(candidate => candidate.Name == name);
            Assert.NotNull(tool);
            Assert.Equal(AiToolKind.Write, tool!.Kind);
        }
    }

    // The report-as-files tool (2026-09-02) mirrors the download endpoints' gate: every internal
    // role can pull the portal's own PDF and workbook; an external login never sees project money.
    [Fact]
    public void ExportValuationReport_isAnInternalRead()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        var tool = Assert.Single(director, candidate => candidate.Name == "export_valuation_report");
        Assert.Equal(AiToolKind.Read, tool.Kind);

        var quantitySurveyor = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor)).Select(t => t.Name);
        Assert.Contains("export_valuation_report", quantitySurveyor);
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name);
        Assert.DoesNotContain("export_valuation_report", subcontractor);
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
            "list_leads", "get_lead", "list_sales_strategies", "get_sales_strategy", "list_rates", "list_clients",
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

        // list_documents (list_drawings until the 2026-09-03 Drawings → Documents rename)
        // deliberately mirrors JpmsRoleSets.DrawingReaders, which ADMITS subcontractors — they
        // read revisions for their assigned work, exactly as over HTTP.
        Assert.Contains("list_documents", director);
        Assert.Contains("list_documents", subcontractor);
        // get_document_extraction (2026-09-07) is the structured read of a revision's PDF behind
        // the same DrawingReaders gate as the page's Extracted-data panel.
        Assert.Contains("get_document_extraction", director);
        Assert.Contains("get_document_extraction", subcontractor);
        Assert.DoesNotContain("list_drawings", director); // the old name is a lookup courtesy, never advertised
    }

    [Fact]
    public void LegacyDrawingNames_resolveToDocumentEntries()
    {
        // 2026-09-03: the register was renamed Drawings → Documents. Saved skills and old habits
        // still say the drawing names — Find lands them on the renamed entry, and the catalogue
        // never lists the old spelling twice.
        Assert.Equal("list_documents", AiToolCatalogue.Find("list_drawings")!.Name);
        Assert.Equal("register_document", AiActionRegistry.Find("register_drawing")!.Name);
        Assert.Equal("file_document_to_project_documents", AiActionRegistry.Find("file_document_as_drawing")!.Name);
        Assert.Equal("set_bid_package_documents", AiActionRegistry.Find("SET_BID_PACKAGE_DRAWINGS")!.Name);
        Assert.Equal("Documents", AiLegacyNames.Current("Drawings"));
        Assert.Contains("delete_drawing", AiLegacyNames.AllNamesFor("delete_document"));
        Assert.DoesNotContain(AiActionRegistry.All, action => action.Name.Contains("drawing", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(AiToolCatalogue.All, tool => tool.Name.Contains("drawing", StringComparison.OrdinalIgnoreCase));
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
    public void MonthEndActions_areDeclared_andGateOnConfirmation()
    {
        // The 2026-08-31 month-end chain (the accountant's ask): sign-off, the Xero coding run,
        // reconciliation and the mappings, all reachable from the connector.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "sign_off_labour_week", "remove_labour_week_sign_off", "run_xero_coding",
            "preview_xero_coding", "reset_xero_coding_outcome",
            "set_xero_line_timesheet_cover", "add_labour_settlement_variance",
            "set_site_xero_mapping", "set_cost_code_xero_mapping"
        })
        {
            Assert.Contains(name, names);
        }

        // Confirm-first is the contract on everything that writes to Xero, freezes a week for
        // settlement, posts a variance, or redirects where money codes — pinned so a rewording
        // never drops the gate.
        foreach (var name in new[]
        {
            "sign_off_labour_week", "run_xero_coding", "reset_xero_coding_outcome",
            "add_labour_settlement_variance", "set_site_xero_mapping", "set_cost_code_xero_mapping"
        })
        {
            Assert.True(AiActionRegistry.All.Single(a => a.Name == name).RequiresConfirmation,
                $"{name} must be confirm-first.");
        }

        // The dry run is read-shaped: it never asks for confirmation (2026-09-03).
        Assert.False(AiActionRegistry.All.Single(a => a.Name == "preview_xero_coding").RequiresConfirmation);

        // The settlement cluster gates on ManageSettlement — a site role never sees it.
        var foreman = UserWith(Role.Foreman);
        Assert.DoesNotContain(AiActionRegistry.All,
            a => a.Name == "run_xero_coding" && a.VisibleTo.IncludesAny(foreman.Roles));
    }

    [Fact]
    public void MonthEndReadTools_exist_andStayInternal()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();
        foreach (var name in new[] { "view_settlement_month", "view_worker_month", "get_xero_mappings", "view_labour_chase" })
        {
            Assert.Contains(name, director);
            Assert.DoesNotContain(name, subcontractor);
        }
    }

    [Fact]
    public void WorkerLinkAndChaseActions_areDeclared()
    {
        // The month-end doc's items A–H (2026-08-31): settlement identity fixable where the gap
        // is found, the backfill sweep, and reasoned chase dismissals with their undo.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "link_worker_to_company", "set_worker_sole_trader",
            "reconcile_worker_directory_links",
            "dismiss_labour_chase_day", "restore_labour_chase_day"
        })
        {
            Assert.Contains(name, names);
        }

        // The bulk sweep writes links for many workers at once — confirm-first, pinned.
        Assert.True(AiActionRegistry.All.Single(a => a.Name == "reconcile_worker_directory_links").RequiresConfirmation);
    }

    [Fact]
    public void AccountantsSeptemberNinthAsks_reachTheConnector()
    {
        // Everything handed to the accountant on 2026-09-09 must be doable from the connector as
        // well as the page: CIS verification, the compliance register, the Xero contact link /
        // pull / push (with its preview), the purchase order email, and the Work Order bill
        // approve / undo with a figure per order.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[]
        {
            "record_cis_verification", "link_directory_record_to_xero_contact",
            "push_directory_contacts_to_xero", "send_work_order_po_email",
            "approve_work_order_bill", "undo_work_order_bill_approval",
            "raise_valuation_invoice_in_xero"
        })
        {
            Assert.Contains(name, names);
        }

        // The Xero raise's 10 Sep follow-ups (Ravenswood Valuation 05): the number of a hand-raised
        // invoice is recorded from the connector too, the raise and issue take their new arguments,
        // and the preview tells the model to STOP on a missing contact mapping — never to create one.
        Assert.Contains("record_valuation_invoice_xero_number", names);
        var recordNumber = AiActionRegistry.All.Single(a => a.Name == "record_valuation_invoice_xero_number");
        Assert.Contains(nameof(RecordValuationInvoiceXeroNumber.RecordedBy), recordNumber.EmailStamps);
        Assert.True(recordNumber.VisibleTo.IncludesAny(UserWith(Role.FinanceDirector).Roles));
        Assert.False(recordNumber.VisibleTo.IncludesAny(UserWith(Role.Foreman).Roles));
        var issueSchema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(AiActionRegistry.Find("issue_valuation_invoice")!));
        Assert.Contains("xeroInvoiceNumber", issueSchema);
        var raiseSchema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(AiActionRegistry.Find("raise_valuation_invoice_in_xero")!));
        Assert.Contains("invoiceDate", raiseSchema);
        Assert.Contains("dueDate", raiseSchema);
        Assert.DoesNotContain("raisedBy", raiseSchema);
        var projectSchema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(AiActionRegistry.Find("update_project_details")!));
        Assert.Contains("xeroContactId", projectSchema);
        var preview = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector)).Single(t => t.Name == "preview_valuation_invoice_xero_raise");
        Assert.Contains("STOP", preview.Description);
        Assert.DoesNotContain("created with the invoice", preview.Description);
        Assert.DoesNotContain("created with the invoice", AiActionRegistry.Find("raise_valuation_invoice_in_xero")!.Description);

        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();
        foreach (var name in new[] { "list_compliance_register", "preview_xero_contact_push", "preview_valuation_invoice_xero_raise", "list_xero_ledger_lines", "read_source" })
        {
            Assert.Contains(name, financeDirector);
            Assert.DoesNotContain(name, subcontractor);
        }

        // The accountant's 10 Sep evening ask: the public liability figure on the register. The
        // read carries it per document and per company with the below-£5m flag and filter; the
        // figure on a document already on file is corrected from the connector too, never by a
        // re-upload; and search_directory says it beside the standing.
        Assert.Contains("set_compliance_document_details", names);
        var setDetails = AiActionRegistry.All.Single(a => a.Name == "set_compliance_document_details");
        Assert.True(setDetails.VisibleTo.IncludesAny(UserWith(Role.FinanceDirector).Roles));
        Assert.False(setDetails.VisibleTo.IncludesAny(UserWith(Role.Subcontractor).Roles));
        var detailsSchema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(setDetails));
        Assert.Contains("publicLiabilityCover", detailsSchema);
        Assert.Contains("expiresAt", detailsSchema);
        var register = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector)).Single(t => t.Name == "list_compliance_register");
        Assert.Contains("belowPublicLiabilityRequirement", System.Text.Json.JsonSerializer.Serialize(register.InputSchema));
        Assert.Contains("£5m", register.Description);
        var fileToSubcontractorSchema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(AiActionRegistry.Find("file_document_to_subcontractor")!));
        Assert.Contains("publicLiabilityCover", fileToSubcontractorSchema);

        // Approve writes tracking to Xero and approves the bill there; undo clears the tracking.
        // Both confirm-first, and the FD's button, never the site's.
        foreach (var name in new[] { "approve_work_order_bill", "undo_work_order_bill_approval", "raise_valuation_invoice_in_xero" })
        {
            var action = AiActionRegistry.All.Single(a => a.Name == name);
            Assert.True(action.RequiresConfirmation, $"{name} must be confirm-first.");
            Assert.True(action.VisibleTo.IncludesAny(UserWith(Role.FinanceDirector).Roles));
            Assert.False(action.VisibleTo.IncludesAny(UserWith(Role.Foreman).Roles));
        }

        // The actor is stamped server-side — never a schema property the model could supply.
        var approve = AiActionRegistry.All.Single(a => a.Name == "approve_work_order_bill");
        var schema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(approve));
        Assert.DoesNotContain("approvedBy", schema);
        Assert.Contains("slices", schema);
    }

    [Fact]
    public void MdsSeptemberEleventhAsk_paymentsReadFromXero_reachTheConnector()
    {
        // "The portal says it can't recognise when a sales invoice is paid" (2026-09-11). Xero is
        // the home of what has been paid, so the connector READS it: every sales invoice on the
        // project's contact in any status (PAID included), the payment-sync preview, and the sync
        // itself — confirm-first, SyncedBy stamped, the FD's tool and never the site's.
        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector));
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();
        foreach (var name in new[] { "list_xero_sales_invoices", "preview_valuation_invoice_payment_sync" })
        {
            var tool = Assert.Single(financeDirector, candidate => candidate.Name == name);
            Assert.Equal(AiToolKind.Read, tool.Kind);
            Assert.Contains("projectId", System.Text.Json.JsonSerializer.Serialize(tool.InputSchema));
            Assert.DoesNotContain(name, subcontractor);
        }
        var list = financeDirector.Single(t => t.Name == "list_xero_sales_invoices");
        Assert.Contains("PAID", list.Description);
        Assert.Contains("record_valuation_invoice_xero_number", list.Description);
        Assert.Contains("never by asking the user", list.Description);
        var preview = financeDirector.Single(t => t.Name == "preview_valuation_invoice_payment_sync");
        Assert.Contains("Ambiguous", preview.Description);
        Assert.Contains("sync_valuation_invoice_payments_from_xero", preview.Description);

        var sync = AiActionRegistry.Find("sync_valuation_invoice_payments_from_xero");
        Assert.NotNull(sync);
        Assert.Equal("Valuation invoices", sync!.Area);
        Assert.True(sync.RequiresConfirmation, "the sync records payments — confirm-first.");
        Assert.Contains(nameof(SyncValuationInvoicePaymentsFromXero.SyncedBy), sync.EmailStamps);
        Assert.True(sync.VisibleTo.IncludesAny(UserWith(Role.FinanceDirector).Roles));
        Assert.False(sync.VisibleTo.IncludesAny(UserWith(Role.Foreman).Roles));
        Assert.Contains("preview_valuation_invoice_payment_sync", sync.Notes);
        Assert.Contains("Never ask the user whether an invoice was paid", sync.Notes);
        var schema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(sync));
        Assert.Contains("projectId", schema);
        Assert.DoesNotContain("syncedBy", schema);
    }

    [Fact]
    public void BidPackageContext_handsOverTheIdsItsActionsTake()
    {
        // The accountant's ask (2026-09-10): the tender list came back as company + status only,
        // so decline_bid_package_recipient could never be given the recipientId it wants. Every
        // list on the context now carries the id the matching action takes, and the actions'
        // notes name the field rather than "the recipient list".
        var context = AiToolCatalogue.ForConnector(UserWith(Role.QuantitySurveyor))
            .Single(tool => tool.Name == "get_bid_package_context");
        foreach (var handle in new[] { "recipientId", "subcontractorId", "quoteId", "lineItemId", "drawingId" })
        {
            Assert.Contains(handle, context.Description);
        }

        foreach (var name in new[] { "decline_bid_package_recipient", "remove_bid_package_recipient" })
        {
            Assert.Contains("tenderList[].recipientId", AiActionRegistry.Find(name)!.Notes);
        }
        Assert.Contains("tenderList[].subcontractorId", AiActionRegistry.Find("submit_quote_for_bid_package")!.Notes);
        Assert.Contains("quotes[].quoteId", AiActionRegistry.Find("revise_quote")!.Notes);
    }

    [Fact]
    public void TodoBrief_reachesTheConnector_asATableRead()
    {
        // The accountant's ask (2026-09-10): "show me the to-do for Ravenswood" must come back as
        // what is open AND what clears each item — a read every internal role gets (the To-do
        // tab's own gate), never an external login, and one whose description tells the model
        // to answer as a table with the portal's nextStep, not to re-derive it from list_todos.
        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector));
        var tool = Assert.Single(financeDirector, candidate => candidate.Name == "get_todo_brief");
        Assert.Equal(AiToolKind.Read, tool.Kind);
        Assert.Contains("TABLE", tool.Description);
        Assert.Contains("nextStep", tool.Description);
        Assert.Contains("not list_todos", tool.Description);
        Assert.DoesNotContain("get_todo_brief",
            AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name));

        var schema = System.Text.Json.JsonSerializer.Serialize(tool.InputSchema);
        Assert.Contains("projectId", schema);
        Assert.Contains("role", schema);
        Assert.Contains("includeDone", schema);
    }

    [Fact]
    public void AccountantsSeptemberFourteenthAsk_supplierAccount_reachesTheConnector()
    {
        // "Is there a way I can have the work orders as this sort of summary… and show the invoice
        // numbers as well, linked to these work orders — they have over-invoiced" (2026-09-14).
        // The Work orders tab's Supplier account is a read every internal role gets (the tab's own
        // gate), never an external login: orders with their lines, every invoice received (awaiting
        // approval included) with its CIS split and matches, and the over-invoice spelled out.
        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector));
        var tool = Assert.Single(financeDirector, candidate => candidate.Name == "get_project_supplier_account");
        Assert.Equal(AiToolKind.Read, tool.Kind);
        Assert.Contains("over-invoice", tool.Description);
        Assert.Contains("awaiting approval", tool.Description);
        Assert.Contains("labour", tool.Description);
        Assert.DoesNotContain("get_project_supplier_account",
            AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name));

        var schema = System.Text.Json.JsonSerializer.Serialize(tool.InputSchema);
        Assert.Contains("projectId", schema);
        Assert.Contains("supplier", schema);
    }

    [Fact]
    public void BookkeepersSeptemberFourteenthAsk_theWholeBill_reachesTheConnector()
    {
        // "Can the description be pulled through from Dext so Nigel knows who it came from?"
        // (2026-09-14). The Dext description lands on the bill in Xero — a line's description or
        // the reference — and the Invoice document window now shows the whole bill for it. The
        // connector reads the same bill whole: list_xero_ledger_lines takes xeroInvoiceId, and
        // its description tells the model to read every line before saying a bill carries no note.
        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector));
        var tool = Assert.Single(financeDirector, candidate => candidate.Name == "list_xero_ledger_lines");
        Assert.Contains("xeroInvoiceId", System.Text.Json.JsonSerializer.Serialize(tool.InputSchema));
        Assert.Contains("Dext description", tool.Description);
        Assert.Contains("read the whole bill", tool.Description);
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

    [Fact]
    public void ListAuditTrail_reachesTheConnector_behindTheEndpointsGate()
    {
        // 2026-09-14, the MD's rule: the connector mirrors the site — what /audit shows, the
        // assistant can read. The catalogue offers it to everyone internal (one record's own
        // history opens that wide) and to no external; the per-read gate is AuditReadGate,
        // the same rule the endpoint applies.
        foreach (var role in new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.SiteManager })
            Assert.Contains("list_audit_trail", AiToolCatalogue.ForConnector(UserWith(role)).Select(t => t.Name));
        foreach (var role in new[] { Role.Subcontractor, Role.Architect, Role.Client })
            Assert.DoesNotContain("list_audit_trail", AiToolCatalogue.ForConnector(UserWith(role)).Select(t => t.Name));

        var siteManager = UserWith(Role.SiteManager);
        Assert.False(Jewel.JPMS.Api.Features.Audit.AuditReadGate.Allows(siteManager, recordId: null, eventType: null));
        Assert.True(Jewel.JPMS.Api.Features.Audit.AuditReadGate.Allows(siteManager, recordId: "wo-1", eventType: null));
        Assert.True(Jewel.JPMS.Api.Features.Audit.AuditReadGate.Allows(UserWith(Role.ProjectManager), recordId: null, eventType: null));
        Assert.False(Jewel.JPMS.Api.Features.Audit.AuditReadGate.Allows(UserWith(Role.FinanceDirector), recordId: null, eventType: AuditEventType.KpiEmailMarked));
        Assert.True(Jewel.JPMS.Api.Features.Audit.AuditReadGate.Allows(UserWith(Role.Admin), recordId: null, eventType: AuditEventType.KpiEmailMarked));
    }

    [Fact]
    public void MailboxTools_stateTheirScope_andCarryTheEnvelope()
    {
        // 2026-09-15, the MD's Portal-vs-Outlook note: a search row showing only the sender and a
        // received time was read as "a file store that only shows what has been filed". The reads
        // are the live projects mailbox, every folder — the descriptions say so, and a row carries
        // the envelope and Graph's send time so nobody has to guess who an email went to.
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        foreach (var name in new[] { "search_mailbox", "list_triage_queue", "get_mailbox_message" })
        {
            var tool = Assert.Single(director, candidate => candidate.Name == name);
            Assert.Contains("projects mailbox", tool.Description);
            Assert.Contains("nothing is stored in the portal", tool.Description);
            Assert.Contains("Sent Items", tool.Description);
            Assert.Contains("own account", tool.Description);
        }
        Assert.Contains("get_mailbox_message", director.Single(t => t.Name == "search_mailbox").Description);
        Assert.Contains("bcc", director.Single(t => t.Name == "get_mailbox_message").Description);

        var message = new MailboxMessage(
            "id", "imid", "paul@example.com", "Paul", "17A", "preview", false,
            new DateTimeOffset(2026, 9, 11, 8, 54, 0, TimeSpan.Zero), Array.Empty<string>(),
            To: new[] { "nigel@example.com" }, Cc: new[] { "projects@jewelbb.co.uk" },
            SentAt: new DateTimeOffset(2026, 9, 11, 8, 53, 0, TimeSpan.Zero));
        var row = System.Text.Json.JsonSerializer.Serialize(AiMailboxTools.Row(message));
        Assert.Contains("\"to\":[\"nigel@example.com\"]", row);
        Assert.Contains("\"cc\":[\"projects@jewelbb.co.uk\"]", row);
        Assert.Contains("\"SentAt\":\"2026-09-11T08:53:00+00:00\"", row);

        var detail = new MailboxMessageDetail("id", "", false, Array.Empty<IntakeAttachment>(), Bcc: new[] { "fd@example.com" });
        Assert.Equal(new[] { "fd@example.com" }, detail.Bcc);
    }

    [Fact]
    public void SalesPaneAndEstimates_reachTheConnector()
    {
        // 2026-09-15, Nigel: an estimate enquiry forwarded to the projects mailbox is tagged on the
        // Sales pane to the lead it is about — an existing one (file_email_to_record, type Lead) or
        // a new one raised from the email — so the assistant reads the enquiry and opens an
        // estimate on the lead. Every button the pane and the lead page get, the connector gets.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[] { "create_lead_from_message", "create_estimate", "update_estimate_details", "move_estimate_status" })
        {
            Assert.Contains(name, names);
            Assert.Equal("Sales", AiActionRegistry.Find(name)!.Area);
        }

        // Raising a lead from an email is a triage decision: the Control Centre's roles, confirm-first,
        // and the notes send the model to file_email_to_record for a lead that already exists.
        var raise = AiActionRegistry.Find("create_lead_from_message")!;
        Assert.True(raise.RequiresConfirmation);
        Assert.True(raise.VisibleTo.IncludesAny(UserWith(Role.ProjectManager).Roles));
        Assert.False(raise.VisibleTo.IncludesAny(UserWith(Role.Foreman).Roles));
        Assert.Contains("file_email_to_record", raise.Notes);
        var raiseSchema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(raise));
        Assert.Contains("messageId", raiseSchema);
        Assert.DoesNotContain("projectId", raiseSchema);

        // The estimate actions take the estimateId, never the reference, and stamp the actor.
        foreach (var name in new[] { "update_estimate_details", "move_estimate_status" })
        {
            Assert.Contains("estimateId", AiActionRegistry.Find(name)!.Notes);
            Assert.Contains("never the reference", AiActionRegistry.Find(name)!.Notes);
        }
        Assert.Contains("CreatedByEmail", AiActionRegistry.Find("create_estimate")!.EmailStamps);
        Assert.Contains("ChangedByEmail", AiActionRegistry.Find("move_estimate_status")!.EmailStamps);
        Assert.True(AiActionRegistry.Find("move_estimate_status")!.RequiresConfirmation);
        Assert.False(AiActionRegistry.Find("create_estimate")!.VisibleTo.IncludesAny(UserWith(Role.Subcontractor).Roles));

        // The reads: get_lead carries the estimates, the record type "lead" reads its mail, and
        // find_by_reference resolves LD-#### and EST-####.
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        Assert.Contains("estimates", director.Single(t => t.Name == "get_lead").Description);
        Assert.Contains("lead", director.Single(t => t.Name == "read_record_emails").Description);
        Assert.Contains("LD-0007", director.Single(t => t.Name == "find_by_reference").Description);
        Assert.Contains("EST-0003", director.Single(t => t.Name == "find_by_reference").Description);
        Assert.True(AiRecordTools.TryMapRecordType("lead", out var lead) && lead == RecordType.Lead);
        Assert.True(AiRecordTools.TryMapRecordType("LD", out var ld) && ld == RecordType.Lead);
    }

    [Fact]
    public void EstimateHouseModel_reachesTheConnector_asTheDefinitionObject()
    {
        // 2026-09-16: set_house_model is confirm-first, the actor stamped, the sales team's, and its
        // model argument is the definition OBJECT, never an escaped string; get_lead hands it back.
        var action = AiActionRegistry.Find("set_house_model");
        Assert.NotNull(action);
        Assert.Equal("Sales", action!.Area);
        Assert.True(action.RequiresConfirmation);
        Assert.Contains("ChangedByEmail", action.EmailStamps);
        Assert.Contains("estimateId", action.Notes);
        Assert.Contains("never the reference", action.Notes);
        Assert.Contains("jpms-house-model", action.Notes);
        var salesDesk = UserWith(Role.SalesMarketing).Roles;
        var subcontractor = UserWith(Role.Subcontractor).Roles;
        Assert.True(action.VisibleTo.IncludesAny(salesDesk));
        Assert.False(action.VisibleTo.IncludesAny(subcontractor));
        var schema = JsonSerializer.Serialize(AiActionSchema.InputSchema(action));
        Assert.Contains("\"model\":{\"type\":\"object\"}", schema);
        Assert.Contains("source", schema);
        Assert.DoesNotContain("changedByEmail", schema);

        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        Assert.Contains("houseModel", director.Single(t => t.Name == "get_lead").Description);
    }

    [Fact]
    public void DeleteLead_reachesTheConnector_forDirectorsOnly()
    {
        // 2026-09-15, Nigel: "I need to be able to delete sales leads" — the lead page's Delete
        // and the connector's delete_lead are the same command with the same gate: the deciders
        // (directors, FD; admins carry every role), confirm-first, the actor stamped, never a Won
        // lead (the handler refuses). The sales team can capture and edit but not delete.
        var action = AiActionRegistry.Find("delete_lead");
        Assert.NotNull(action);
        Assert.Equal("Sales", action!.Area);
        Assert.True(action.RequiresConfirmation);
        Assert.Contains("DeletedByEmail", action.EmailStamps);
        Assert.True(action.VisibleTo.IncludesAny(UserWith(Role.ManagingDirector).Roles));
        Assert.True(action.VisibleTo.IncludesAny(UserWith(Role.FinanceDirector).Roles));
        Assert.False(action.VisibleTo.IncludesAny(UserWith(Role.SalesMarketing).Roles));
        Assert.False(action.VisibleTo.IncludesAny(UserWith(Role.ProjectManager).Roles));
        Assert.Contains("Won", action.Description);
        Assert.Contains("Lost", action.Description);
        var schema = System.Text.Json.JsonSerializer.Serialize(AiActionSchema.InputSchema(action));
        Assert.Contains("leadId", schema);
        Assert.DoesNotContain("deletedByEmail", schema);
    }

    [Fact]
    public void SalesProposals_reachTheConnector()
    {
        // 2026-09-15, Nigel: "ingest the emails and attachments and prepare a proposal" — the
        // lead page's Proposals panel (save a draft, send it, withdraw it) over the connector, with
        // get_lead handing over the proposalId the send and withdraw take. Sending emails the
        // prospect, so it is confirm-first; withdrawing is the directors' call.
        var names = AiActionRegistry.All.Select(a => a.Name).ToList();
        foreach (var name in new[] { "save_sales_proposal", "send_sales_proposal", "withdraw_sales_proposal" })
        {
            Assert.Contains(name, names);
            Assert.Equal("Sales", AiActionRegistry.Find(name)!.Area);
        }
        Assert.Contains("SavedByEmail", AiActionRegistry.Find("save_sales_proposal")!.EmailStamps);
        Assert.Contains("read_record_emails", AiActionRegistry.Find("save_sales_proposal")!.Notes);
        Assert.False(AiActionRegistry.Find("save_sales_proposal")!.RequiresConfirmation);
        Assert.True(AiActionRegistry.Find("send_sales_proposal")!.RequiresConfirmation);
        Assert.Contains("proposalId", AiActionRegistry.Find("send_sales_proposal")!.Notes);
        Assert.True(AiActionRegistry.Find("withdraw_sales_proposal")!.RequiresConfirmation);
        Assert.False(AiActionRegistry.Find("withdraw_sales_proposal")!.VisibleTo.IncludesAny(UserWith(Role.ProjectManager).Roles));
        Assert.True(AiActionRegistry.Find("withdraw_sales_proposal")!.VisibleTo.IncludesAny(UserWith(Role.ManagingDirector).Roles));

        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector));
        Assert.Contains("proposals", director.Single(t => t.Name == "get_lead").Description);
    }

    [Fact]
    public void AccountantsSeptemberFifteenthAsks_theLookupsTheCreatesNeed_reachTheConnector()
    {
        // The accountant's 2026-09-15 attempt to raise a work order on a supplier not yet in the
        // directory stalled twice on a LOOKUP, not a write: add_subcontractor_to_directory wanted
        // tradeIds "from list_trades" (a tool the notes named but nobody had built), and
        // import_xero_supplier wanted a Xero ContactID the model could only get by asking the user
        // to paste it from Xero's URL. Both reads now exist, gated as their pages are, and the
        // actions' notes send the model to them.
        var financeDirector = AiToolCatalogue.ForConnector(UserWith(Role.FinanceDirector)).Select(t => t.Name).ToList();
        Assert.Contains("list_trades", financeDirector);
        Assert.Contains("list_xero_suppliers", financeDirector);
        Assert.Contains("list_trades", AiToolCatalogue.ForConnector(UserWith(Role.Foreman)).Select(t => t.Name));
        Assert.DoesNotContain("list_xero_suppliers", AiToolCatalogue.ForConnector(UserWith(Role.Foreman)).Select(t => t.Name));
        Assert.DoesNotContain("list_trades", AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name));
        Assert.Equal(AiToolKind.Read, AiToolCatalogue.Find("list_trades")!.Kind);
        Assert.Equal(AiToolKind.Read, AiToolCatalogue.Find("list_xero_suppliers")!.Kind);

        Assert.Contains("list_trades", AiActionRegistry.Find("add_subcontractor_to_directory")!.Notes);
        Assert.Contains("list_xero_suppliers", AiActionRegistry.Find("import_xero_supplier")!.Notes);
        Assert.Contains("list_xero_suppliers", AiActionRegistry.Find("link_directory_record_to_xero_contact")!.Notes);
    }

    [Fact]
    public void EveryToolOrActionACatalogueTextNames_exists()
    {
        // The general lesson of 2026-09-15: a description or note that sends the model to a tool
        // is a promise. When the name resolves to nothing, the model tells the user the portal
        // cannot do the thing — and it is right, from where it sits. So every snake_case name that
        // reads like a tool or action in any tool description, action description, action note or
        // input schema must resolve through the catalogue or the action registry, legacy names
        // included. What counts as a name is ConnectorNameReferences, shared with the guard over
        // the skills, which name the same doors and drifted the same way on 2026-09-17.
        var texts = AiToolCatalogue.All
            .SelectMany(tool => new[]
            {
                (Where: "tool " + tool.Name, Text: tool.Description),
                (Where: "tool " + tool.Name + " schema", Text: System.Text.Json.JsonSerializer.Serialize(tool.InputSchema))
            })
            .Concat(AiActionRegistry.All.SelectMany(action => new[]
            {
                (Where: "action " + action.Name, Text: action.Description),
                (Where: "action " + action.Name + " notes", Text: action.Notes ?? "")
            }));

        var dangling = texts
            .SelectMany(entry => ConnectorNameReferences.DanglingIn(entry.Where, entry.Text))
            .Distinct()
            .ToList();

        Assert.True(dangling.Count == 0, "Catalogue text names tools that do not exist:\n" + string.Join("\n", dangling));
    }

    [Fact]
    public void EveryEmailTheHasPagesSends_reachesTheConnector_withItsSaveAsDraftRoute()
    {
        // 2026-09-17 gave nine record doors a Send; 2026-09-18 closed the three the connector
        // still lacked, and made resend_request_document do what its name had always said — it
        // queued a background draft and told the model nothing about it. The subcontractor
        // statement was skipped because nothing dispatched it — the command had a handler, gates
        // and a DI registration and NO endpoint, so the page's
        // own button 404'd; the programme reply because its gate was a private field of its
        // endpoint; the composer's invite because it had no save-as-draft at all and nobody had
        // settled whether the assistant should get a door that always sends (Nigel: grow one).
        foreach (var name in new[]
        {
            "send_request_email", "send_request_emails", "send_request_reply",
            "send_work_order_po_email", "send_valuation_report_snapshot_email",
            "send_bid_package_invite_to_tender_list", "send_bid_package_invite",
            "send_subcontractor_statement_email", "send_programme_reply", "send_defect_to_supplier",
            "send_variation_order_email", "resend_request_document"
        })
        {
            var action = AiActionRegistry.Find(name);
            Assert.NotNull(action);
            Assert.Contains("SENDS EMAIL", action!.Description);
            Assert.True(action.RequiresConfirmation, name + " sends to someone outside Jewel");
        }

        // The review route, named in the description so the model offers it rather than inventing
        // one. resend_request_document has none: it is send_request_email under an older name and
        // that is where a caller who wants a draft is sent.
        foreach (var name in new[]
        {
            "send_work_order_po_email", "send_valuation_report_snapshot_email",
            "send_bid_package_invite_to_tender_list", "send_bid_package_invite",
            "send_subcontractor_statement_email", "send_programme_reply", "send_variation_order_email"
        })
            Assert.Contains("saveAsDraftOnly", AiActionRegistry.Find(name)!.Description);

        // Each new door's reach is its page's, not a wider one.
        var statement = AiActionRegistry.Find("send_subcontractor_statement_email")!;
        Assert.True(statement.VisibleTo.Includes(Role.FinanceDirector));
        Assert.False(statement.VisibleTo.Includes(Role.Foreman));

        var programme = AiActionRegistry.Find("send_programme_reply")!;
        Assert.True(programme.VisibleTo.Includes(JpmsRoles.SiteManager));
        Assert.False(programme.VisibleTo.Includes(JpmsRoles.Architect));

        // The variation goes TO the client, so the client is on its approval gate and not on this
        // one — the circle that manages variations sends it.
        var variation = AiActionRegistry.Find("send_variation_order_email")!;
        Assert.True(variation.VisibleTo.Includes(JpmsRoles.Estimator));
        Assert.False(variation.VisibleTo.Includes(JpmsRoles.Client));
    }

    [Fact]
    public void HsAudits_reachTheConnector_behindTheirPagesGates()
    {
        // 2026-09-15: the H&S site audit (Katy-Louise's workbook brought into the portal) and the
        // register it mints onto. The reads mirror the pages' gates — the whole internal team
        // reads audits and the register; a foreman reads them, a subcontractor does not — and the
        // writes are the auditors' (the H&S officer, the directors, the PM, the site manager, the
        // compliance coordinator). Issue is the declaration that mints corrective actions, so it
        // is confirm-first; the register write's notes send the model to the new read.
        var officer = AiToolCatalogue.ForConnector(UserWith(Role.HealthSafetyOfficer)).Select(t => t.Name).ToList();
        Assert.Contains("list_hs_audits", officer);
        Assert.Contains("get_hs_audit", officer);
        Assert.Contains("list_hs_records", officer);
        Assert.Contains("list_hs_audits", AiToolCatalogue.ForConnector(UserWith(Role.Foreman)).Select(t => t.Name));
        Assert.DoesNotContain("list_hs_audits", AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name));
        Assert.Equal(AiToolKind.Read, AiToolCatalogue.Find("get_hs_audit")!.Kind);

        foreach (var name in new[] { "create_hs_audit", "update_hs_audit_details", "update_hs_audit_items", "issue_hs_audit", "close_hs_audit" })
        {
            var action = AiActionRegistry.Find(name);
            Assert.NotNull(action);
            Assert.True(action!.VisibleTo.Includes(Role.HealthSafetyOfficer), name);
            Assert.True(action.VisibleTo.Includes(Role.ManagingDirector), name);
            Assert.False(action.VisibleTo.Includes(Role.Foreman), name);
        }
        Assert.True(AiActionRegistry.Find("issue_hs_audit")!.RequiresConfirmation);
        Assert.Contains("IssuedByEmail", AiActionRegistry.Find("issue_hs_audit")!.EmailStamps);
        Assert.Contains("list_hs_records", AiActionRegistry.Find("log_hs_record")!.Notes);
        Assert.Contains("assignedToName", AiActionRegistry.Find("log_hs_record")!.Notes);
    }
    // 2026-09-16: update_project_details overwrites every field, so it needs a read that returns
    // them all — list_projects gives only id, reference, name and stage. get_project_details is
    // that read, behind the Project settings page's gate, and the action's notes send the model
    // to it before every call.
    [Fact]
    public void ProjectDetails_areReadable_beforeTheyAreOverwritten()
    {
        var read = AiToolCatalogue.Find("get_project_details");
        Assert.NotNull(read);
        Assert.Equal(AiToolKind.Read, read!.Kind);
        foreach (var editor in new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager })
            Assert.Contains("get_project_details", AiToolCatalogue.ForConnector(UserWith(editor)).Select(t => t.Name));
        Assert.DoesNotContain("get_project_details", AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name));

        var update = AiActionRegistry.Find("update_project_details");
        Assert.NotNull(update);
        Assert.Contains("get_project_details", update!.Description);
        Assert.Contains("get_project_details", update.Notes);
        foreach (var role in Enum.GetValues<Role>())
            Assert.Equal(update.VisibleTo.Includes(role), read.VisibleTo.Includes(role));
    }

    // 2026-09-16: "whenever drawings are triaged, transcribe them into data so work can be done
    // over them without using a lot of AI context up". The extraction summary stays behind the
    // DrawingReaders gate; the rows are a separate, filtered read behind the same gate; queueing a
    // read and rebuilding the rows are actions for the roles that may extract on the page.
    [Fact]
    public void DrawingTranscription_isQueryableRows_notTheWholeBlob()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        var subcontractor = AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name).ToList();
        foreach (var name in new[] { "get_document_extraction", "query_document_data" })
        {
            Assert.Contains(name, director);
            Assert.Contains(name, subcontractor);
        }

        var summary = AiToolCatalogue.Find("get_document_extraction")!;
        Assert.Contains("query_document_data", summary.Description);
        Assert.Contains("NOT in this reply", summary.Description);
        var query = AiToolCatalogue.Find("query_document_data")!;
        Assert.Equal(AiToolKind.Read, query.Kind);
        var querySchema = System.Text.Json.JsonSerializer.Serialize(query.InputSchema);
        foreach (var argument in new[] { "kind", "projectId", "contains", "nearX", "withinMm", "limit", "offset" })
            Assert.Contains(argument, querySchema);

        foreach (var name in new[] { "extract_document_data", "rebuild_document_data" })
        {
            var action = AiActionRegistry.Find(name);
            Assert.NotNull(action);
            Assert.True(action!.VisibleTo.Includes(Role.ManagingDirector), name);
            Assert.True(action.VisibleTo.Includes(Role.ProjectManager), name);
            Assert.False(action.VisibleTo.Includes(Role.Subcontractor), name);
            Assert.False(action.RequiresConfirmation, name);
        }
        Assert.Contains("query_document_data", AiActionRegistry.Find("rebuild_document_data")!.Description);
    }

    // 2026-09-16: the one thing that moves a defect along — putting it to the company that has to
    // fix it — reaches the connector. Confirm-first (it emails a third party), the caller stamped
    // as the sender, and list_defects carries the exact wording the action will send.
    [Fact]
    public void SendDefectToSupplier_reachesTheConnector_confirmFirst()
    {
        var action = AiActionRegistry.Find("send_defect_to_supplier");
        Assert.NotNull(action);
        Assert.True(action!.RequiresConfirmation);
        Assert.Contains("SentByEmail", action.EmailStamps);
        Assert.True(action.VisibleTo.Includes(Role.ManagingDirector));
        Assert.True(action.VisibleTo.Includes(Role.SiteManager));
        Assert.False(action.VisibleTo.Includes(Role.Subcontractor));
        Assert.Contains("list_defects", action.Notes);
        Assert.Contains("supplierEmail", action.Notes);
        Assert.Contains("send_defect_to_supplier", AiToolCatalogue.Find("list_defects")!.Description);
    }

    // The weekly Contractor's Report (2026-09-16): the register and the composed document are
    // readable, the entered fields writable, and delete is confirm-first — a page-only feature is
    // a gap the site team finds first.
    [Fact]
    public void ContractorsReports_reachTheConnector()
    {
        var director = AiToolCatalogue.ForConnector(UserWith(Role.ManagingDirector)).Select(t => t.Name).ToList();
        Assert.Contains("list_contractors_reports", director);
        Assert.Contains("get_contractors_report", director);
        Assert.DoesNotContain("get_contractors_report", AiToolCatalogue.ForConnector(UserWith(Role.Subcontractor)).Select(t => t.Name));
        Assert.Contains("findings", AiToolCatalogue.Find("get_contractors_report")!.Description);

        var create = AiActionRegistry.Find("create_contractors_report");
        Assert.NotNull(create);
        Assert.Contains("CreatedByEmail", create!.EmailStamps);
        Assert.True(create.VisibleTo.Includes(Role.SiteManager));
        Assert.False(create.VisibleTo.Includes(Role.Client));

        var update = AiActionRegistry.Find("update_contractors_report");
        Assert.NotNull(update);
        Assert.Contains("get_contractors_report", update!.Notes);

        var delete = AiActionRegistry.Find("delete_contractors_report");
        Assert.NotNull(delete);
        Assert.True(delete!.RequiresConfirmation);
    }
}
