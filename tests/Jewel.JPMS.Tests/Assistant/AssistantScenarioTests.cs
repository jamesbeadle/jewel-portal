using System.Text.Json;
using ClosedXML.Excel;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests.Assistant;

// The live failures, replayed against the real runner (docs/ai/06-context-retrieval.md, Phase 4).
// Each scenario scripts exactly the tool calls the model made on the day, seeds the data that was
// on the portal, and asserts what the server now does about it — the refusal, the rewrite, the
// evidence handed back. When one of these goes red, a guard has regressed; when a new failure
// arrives, it becomes the next scenario here.
public sealed class AssistantScenarioTests
{
    private const string ByFrance = "3490f944b29545c4b8d5a04130f42ab8";
    private const string AbbotRoad = "7d1c5e0a2b3f4c6d8e9f0a1b2c3d4e5f";

    private static async Task SeedProjectsAsync(AssistantHarness harness)
    {
        harness.Db.Projects.AddRange(
            new ProjectEntity { ProjectId = ByFrance, Reference = "JBB-2026-001", Name = "By France", Stage = (int)ProjectStage.LiveDelivery },
            new ProjectEntity { ProjectId = AbbotRoad, Reference = "JBB-2026-002", Name = "Abbot Road", Stage = (int)ProjectStage.LiveDelivery });
        await harness.Db.SaveChangesAsync();
    }

    private static AiScope OnAbbotRoadRfis() =>
        new(AbbotRoad, $"/projects/{AbbotRoad}/requests/rfis", "RFIs");

    // ---- 2026-08-25, 13:27: "load by france rfis" from an Abbot Road page ----

    [Fact]
    public async Task LoadByFranceRfis_fromAbbotRoad_theNameInTheRouteIsRewrittenToTheRealId()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Claude
            .Then(ScriptedClaude.Call("navigate_to", new { route = "/projects/By France/requests/rfis", reason = "you asked" }))
            .ThenSay("You're on the By France RFI register.");

        var turn = await harness.SendAsync("load by france rfis", OnAbbotRoadRfis());

        var action = Assert.Single(turn.UiActions);
        Assert.Equal("navigate_to", action.Tool);
        using var arguments = JsonDocument.Parse(action.ArgumentsJson);
        Assert.Equal($"/projects/{ByFrance}/requests/rfis", arguments.RootElement.GetProperty("route").GetString());

        // System.Text.Json escapes non-ASCII (the dash rides as \u2014), so the two halves are
        // asserted separately.
        var result = turn.LastToolResult("navigate_to");
        Assert.Contains("\"ok\":true", result);
        Assert.Contains("JBB-2026-001", result);
        Assert.Contains("By France", result);
        Assert.Equal(AiTurnStatus.Complete, turn.Status);
    }

    [Fact]
    public async Task LoadRfis_withThePlaceholderAndNoProjectInView_isRefusedTowardsListProjects()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Claude
            .Then(ScriptedClaude.Call("navigate_to", new { route = "/projects/{project}/requests/rfis" }))
            .ThenSay("I could not take you there.");

        var turn = await harness.SendAsync("load the rfis", new AiScope(null, "/rfis", "RFI dashboard"));

        Assert.Empty(turn.UiActions);
        var result = turn.LastToolResult("navigate_to");
        Assert.Contains("\"ok\":false", result);
        Assert.Contains("list_projects", result);
        Assert.False(turn.Steps.Single(step => step.Tool == "navigate_to").Ok);
    }

    [Fact]
    public async Task LoadRfis_forAProjectThatDoesNotExist_isRefused_neverANotFoundPage()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Claude
            .Then(ScriptedClaude.Call("navigate_to", new { route = "/projects/Windy Ridge/requests/rfis" }))
            .ThenSay("No such project.");

        var turn = await harness.SendAsync("load windy ridge rfis", OnAbbotRoadRfis());

        Assert.Empty(turn.UiActions);
        Assert.Contains("No project has the id, reference or name", turn.LastToolResult("navigate_to"));
    }

    [Fact]
    public async Task LoadRfis_withThePlaceholder_onAProjectPage_fillsItFromTheProjectInView()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Claude
            .Then(ScriptedClaude.Call("navigate_to", new { route = "/projects/{project}/requests/rfis" }))
            .ThenSay("Here are the RFIs.");

        var turn = await harness.SendAsync("show the rfis", OnAbbotRoadRfis());

        using var arguments = JsonDocument.Parse(Assert.Single(turn.UiActions).ArgumentsJson);
        Assert.Equal($"/projects/{AbbotRoad}/requests/rfis", arguments.RootElement.GetProperty("route").GetString());
    }

    // ---- 2026-08-25, 08:50: "update V01 to the V01 tab" with a multi-tab valuation attached ----

    private static byte[] ValuationWorkbook()
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Valuation No.14");
        summary.Cell(1, 1).Value = "Item";
        summary.Cell(1, 2).Value = "Description";
        summary.Cell(1, 3).Value = "Sum";
        for (var row = 2; row <= 300; row++)
        {
            summary.Cell(row, 1).Value = $"{row - 1}.00";
            summary.Cell(row, 2).Value = $"Contract works item {row - 1}";
            summary.Cell(row, 3).Value = row * 10m;
        }
        summary.Cell(301, 1).Value = "V01";
        summary.Cell(301, 2).Value = "Levelling compound removal";
        summary.Cell(301, 3).Value = 1050m;

        var v01 = workbook.Worksheets.Add("V01 - Levelling compound");
        v01.Cell(1, 1).Value = "Item";
        v01.Cell(1, 2).Value = "Qty";
        v01.Cell(1, 3).Value = "Rate";
        v01.Cell(1, 4).Value = "Total";
        v01.Cell(2, 1).Value = "Levelling compound";
        v01.Cell(2, 2).Value = 150;
        v01.Cell(2, 3).Value = 7m;
        v01.Cell(2, 4).Value = 1050m;
        v01.Cell(2, 4).Style.NumberFormat.Format = "#,##0.00";

        var v02 = workbook.Worksheets.Add("V02 - Additional steel works");
        v02.Cell(1, 1).Value = "Steel";
        v02.Cell(1, 2).Value = 2400m;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    [Fact]
    public async Task MultiTabValuation_everyTabIsOnHand_theV01TabIsFoundByName_andReadsInFull()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var scope = new AiScope(AbbotRoad, $"/projects/{AbbotRoad}/valuation", "Valuation Report");

        var receipt = await harness.AttachAsync("Valuation-No.14-17a-Abbot-Road.xlsx", ValuationWorkbook(), scope);
        Assert.Equal("3 sheets · 304 rows", receipt.Summary);

        var attachment = Assert.Single(harness.Db.AiAttachments);
        var sourceId = "chat:" + attachment.AttachmentId;
        harness.Claude
            .Then(ScriptedClaude.Call("find_in_source", new { query = "V01" }))
            .Then(ScriptedClaude.Call("read_source", new { source_id = sourceId, part = "V01 - Levelling compound" }))
            .ThenSay("V01 on the sheet is £1,050.00.");

        var turn = await harness.SendAsync("update V01 to suit the tab V01 - Levelling compound", scope, receipt.ConversationId);

        // Hop 1: before any tool ran, the model was told what is on hand — every tab, nothing read.
        var firstContext = ScriptedClaude.LastUserText(harness.Claude.Calls[0]);
        Assert.Contains("Files attached to this chat", firstContext);
        Assert.Contains("«V01 - Levelling compound» · 2 rows", firstContext);
        Assert.Contains("«V02 - Additional steel works» · 1 row", firstContext);
        Assert.Contains("Read so far: nothing yet", firstContext);

        // The Context row replays the SHAPE and a preview, never 25k characters of the first sheet.
        var contextRow = turn.Rows.Single(row => row.Role == (int)AiChatRole.Context);
        Assert.Contains("Parts: «Valuation No.14» · 301 rows, «V01 - Levelling compound» · 2 rows", contextRow.Body);
        Assert.True(contextRow.Body.Length < 4_000, $"context row is {contextRow.Body.Length} chars");

        // find_in_source: the tab NAMED for V01 and the summary row that mentions it.
        var found = turn.LastToolResult("find_in_source");
        Assert.Contains("\"parts_by_name\":[{\"part\":\"V01 - Levelling compound\"", found);
        Assert.Contains("Levelling compound removal", found);

        // read_source: the whole tab, row numbers and displayed values, nothing cut.
        var read = turn.LastToolResult("read_source");
        Assert.Contains("\"part\":\"V01 - Levelling compound\"", read);
        Assert.Contains("2\\tLevelling compound\\t150\\t7\\t1,050.00", read);
        Assert.Contains("\"reached_end\":true", read);

        // Hop 3: the turn context now records what was read and searched.
        var thirdContext = ScriptedClaude.LastUserText(harness.Claude.Calls[2]);
        Assert.Contains("Read so far: «V01 - Levelling compound»", thirdContext);
        Assert.Contains("Searched for: «V01»", thirdContext);
        Assert.Equal(AiTurnStatus.Complete, turn.Status);
    }

    [Fact]
    public async Task ReadSource_withAnUnknownPart_listsTheRealParts()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var scope = new AiScope(AbbotRoad, $"/projects/{AbbotRoad}/valuation", "Valuation Report");
        var receipt = await harness.AttachAsync("Valuation.xlsx", ValuationWorkbook(), scope);
        var sourceId = "chat:" + Assert.Single(harness.Db.AiAttachments).AttachmentId;
        harness.Claude
            .Then(ScriptedClaude.Call("read_source", new { source_id = sourceId, part = "V01" }))
            .ThenSay("done");

        var turn = await harness.SendAsync("read V01", scope, receipt.ConversationId);

        var result = turn.LastToolResult("read_source");
        Assert.Contains("\"ok\":false", result);
        Assert.Contains("has no part named", result);
        Assert.Contains("V01 - Levelling compound", result);
        Assert.Contains("(2 rows)", result);
    }

    // ---- 2026-08-25, 08:52: the edit had no write path; now it has one, guarded ----

    private static async Task<string> SeedVariationAsync(AssistantHarness harness, int number, VariationOrderStatus status)
    {
        var order = new VariationOrderEntity
        {
            VariationOrderId = $"vo-{number}",
            ProjectId = AbbotRoad,
            Number = number,
            Reference = $"VOQ-{number:0000}",
            Title = $"Variation {number}",
            Status = (int)status,
            VariationRef = status == VariationOrderStatus.Approved ? $"V{number}" : null,
            Value = status == VariationOrderStatus.Approved ? 450m : 0m,
            CreatedAt = DateTimeOffset.UtcNow
        };
        harness.Db.VariationOrders.Add(order);
        await harness.Db.SaveChangesAsync();
        return order.VariationOrderId;
    }

    [Fact]
    public async Task EditLines_onAnIssuedVariation_isRefusedInTheUsersTerms()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 80, VariationOrderStatus.Issued);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_edit_lines", record_id = id }))
            .ThenSay("V80 is not approved yet.");

        var turn = await harness.SendAsync("update V80's lines", OnAbbotRoadRfis());

        Assert.Empty(turn.UiActions);
        var result = turn.LastToolResult("open_modal");
        Assert.Contains("V80 is Issued, not Approved", result);
    }

    [Fact]
    public async Task EditLines_onAnApprovedVariation_isHandedToTheBrowser_withItsProjectStamped()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 1, VariationOrderStatus.Approved);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_edit_lines", record_id = id }))
            .ThenSay("The dialog is open.");

        // From a whole-company page — no project in view — the server still knows the project.
        var turn = await harness.SendAsync("edit V1's lines", new AiScope(null, "/todos", "To-dos"));

        var action = Assert.Single(turn.UiActions);
        Assert.Equal("open_modal", action.Tool);
        using var arguments = JsonDocument.Parse(action.ArgumentsJson);
        Assert.Equal(AbbotRoad, arguments.RootElement.GetProperty("project_id").GetString());
    }

    [Fact]
    public async Task EditLines_withAnInventedRecordId_isRefused()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_edit_lines", record_id = "made-up" }))
            .ThenSay("no");

        var turn = await harness.SendAsync("edit the lines", OnAbbotRoadRfis());

        Assert.Empty(turn.UiActions);
        Assert.Contains("not a real record id", turn.LastToolResult("open_modal"));
    }

    [Fact]
    public async Task GetVariationContext_resolvesV01_andListsTheApprovedLinesWithTheirIds()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 1, VariationOrderStatus.Approved);
        harness.Db.ValuationLineItems.Add(new ValuationLineItemEntity
        {
            ValuationLineItemId = "line-v1", ProjectId = AbbotRoad, ElementType = (int)ValuationElementType.Variation,
            VariationRef = "V1", VariationTitle = "Variation 1", CostCode = "3.14", Description = "Levelling compound removal",
            Unit = "item", Quantity = 1m, Rate = 450m, LineAmount = 450m, DisplayOrder = 1
        });
        await harness.Db.SaveChangesAsync();
        harness.Claude
            .Then(ScriptedClaude.Call("get_variation_context", new { reference = "V01" }))
            .ThenSay("V1 is approved at £450.");

        var turn = await harness.SendAsync("what is V01", OnAbbotRoadRfis());

        var result = turn.LastToolResult("get_variation_context");
        Assert.Contains($"\"VariationOrderId\":\"{id}\"", result);
        Assert.Contains("\"ValuationLineItemId\":\"line-v1\"", result);
        Assert.Contains("\"approvedValue\":450", result);
        Assert.Contains("variation_edit_lines", result);
    }

    [Fact]
    public async Task GetValuationContext_readsTheSelectedClaimsPercentages_andSaysWhetherItIsDraft()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Db.ValuationLineItems.Add(new ValuationLineItemEntity
        {
            ValuationLineItemId = "line-1", ProjectId = AbbotRoad, ElementType = (int)ValuationElementType.ContractWorks,
            SectionCode = "A", SectionName = "Substructure", CostCode = "1.01", Description = "Excavation",
            Unit = "item", Quantity = 1m, Rate = 10_000m, LineAmount = 10_000m, DisplayOrder = 1
        });
        harness.Db.ValuationClaims.AddRange(
            new ValuationClaimEntity { ValuationClaimId = "claim-1", ProjectId = AbbotRoad, ClaimNumber = 1, Status = (int)ValuationClaimStatus.Confirmed, ClaimDate = DateTimeOffset.UtcNow.AddMonths(-1) },
            new ValuationClaimEntity { ValuationClaimId = "claim-2", ProjectId = AbbotRoad, ClaimNumber = 2, Status = (int)ValuationClaimStatus.Draft, ClaimDate = DateTimeOffset.UtcNow });
        harness.Db.ClaimLines.AddRange(
            new ClaimLineEntity { ClaimLineId = "cl-1", ValuationClaimId = "claim-1", ValuationLineItemId = "line-1", PercentComplete = 40m, CumulativeClaimed = 4_000m, PeriodIncrement = 4_000m },
            new ClaimLineEntity { ClaimLineId = "cl-2", ValuationClaimId = "claim-2", ValuationLineItemId = "line-1", PercentComplete = 65m, CumulativeClaimed = 6_500m, PeriodIncrement = 2_500m });
        await harness.Db.SaveChangesAsync();
        harness.Claude
            .Then(ScriptedClaude.Call("get_valuation_context", new { }))
            .ThenSay("Excavation is 65% complete.");

        var turn = await harness.SendAsync("review the % complete", new AiScope(AbbotRoad, $"/projects/{AbbotRoad}/valuation", "Valuation Report"));

        var result = turn.LastToolResult("get_valuation_context");
        Assert.Contains("\"percentComplete\":65", result);
        Assert.Contains("\"previousPercent\":40", result);
        Assert.Contains("\"periodIncrement\":2500", result);
        Assert.Contains("\"editable\":true", result);
        Assert.Contains("claim_progress", result);
    }

    // ---- The registries the assistant is held together by ----

    [Fact]
    public void TheRegistries_doNotDrift()
    {
        // The API refuses to boot on drift; a build should refuse first.
        AiRegistryDriftCheck.Assert();
    }

    // ---- 2026-08-22: "update the draft VO to these client-agreed details" on V80 (Issued) ----

    [Fact]
    public async Task StageBuildUp_onAnIssuedVariation_isHandedToTheBrowser()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 80, VariationOrderStatus.Issued);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_build_up", record_id = id }))
            .ThenSay("The agreed build-up dialog is open.");

        var turn = await harness.SendAsync("update the draft VO V80 to the client-agreed details", OnAbbotRoadRfis());

        var action = Assert.Single(turn.UiActions);
        Assert.Equal("open_modal", action.Tool);
        using var arguments = JsonDocument.Parse(action.ArgumentsJson);
        Assert.Equal("variation_build_up", arguments.RootElement.GetProperty("modal_key").GetString());
        Assert.Equal(AbbotRoad, arguments.RootElement.GetProperty("project_id").GetString());
    }

    [Fact]
    public async Task StageBuildUp_onAnApprovedVariation_isRefusedTowardsEditLines()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 1, VariationOrderStatus.Approved);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_build_up", record_id = id }))
            .ThenSay("V1 is already approved.");

        var turn = await harness.SendAsync("stage V1's build-up", OnAbbotRoadRfis());

        Assert.Empty(turn.UiActions);
        var result = turn.LastToolResult("open_modal");
        Assert.Contains("V1 is Approved", result);
        Assert.Contains("variation_edit_lines", result);
    }

    // ---- 2026-08-25, 15:10: Fable + a 17-sheet workbook — "That reply took longer than one
    // request allows", twice. The Claude call now runs on the collector's background task and the
    // answer is COLLECTED by a later request (docs/ai/07-reply-collection.md). ----

    [Fact]
    public async Task ASlowReply_outlivesTheInlineWait_andIsCollectedWithTheSameToolRun()
    {
        using var harness = new AssistantHarness(inlineWait: TimeSpan.FromMilliseconds(150));
        await SeedProjectsAsync(harness);
        harness.Claude.ReplyDelay = TimeSpan.FromMilliseconds(600);
        harness.Claude
            .Then(ScriptedClaude.Call("navigate_to", new { route = "/projects/By France/requests/rfis", reason = "you asked" }))
            .ThenSay("You're on the By France RFI register.");

        var turn = await harness.SendAsync("load by france rfis", OnAbbotRoadRfis());

        // Both hops outlived the wait, so each was collected at least once — and the hop that was
        // collected did exactly what the fast path does: the route rewritten, the result stored.
        Assert.True(turn.Collects >= 2, $"expected the slow replies to be collected, saw {turn.Collects} collects");
        var action = Assert.Single(turn.UiActions);
        Assert.Equal("navigate_to", action.Tool);
        Assert.Contains("\"ok\":true", turn.LastToolResult("navigate_to"));
        Assert.Equal(AiTurnStatus.Complete, turn.Status);
        Assert.Equal("You're on the By France RFI register.", turn.Reply);

        // The calls ran on the collector's budget, not the in-request 36s.
        Assert.All(harness.Claude.Budgets, budget => Assert.Equal(AiReplyCollector.CallBudget, budget));

        // Every pending row was consumed exactly once; none is left answered-but-unapplied.
        var pending = harness.Db.AiPendingReplies.AsNoTracking().Where(row => row.ConversationId == turn.ConversationId).ToList();
        Assert.Equal(2, pending.Count);
        Assert.All(pending, row => Assert.Equal(AiPendingReplyStatus.Consumed, row.Status));
        Assert.All(pending, row => Assert.NotNull(row.ReplyJson));
    }

    [Fact]
    public async Task AFastReply_landsInsideTheInlineWait_andNeedsNoCollect()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        harness.Claude.ThenSay("Hello.");

        var turn = await harness.SendAsync("hello", OnAbbotRoadRfis());

        Assert.Equal(0, turn.Collects);
        Assert.Equal(AiTurnStatus.Complete, turn.Status);
        var row = Assert.Single(harness.Db.AiPendingReplies.AsNoTracking().Where(pending => pending.ConversationId == turn.ConversationId));
        Assert.Equal(AiPendingReplyStatus.Consumed, row.Status);
        // The background task recorded the answer on the row before the hop applied it.
        Assert.NotNull(row.AnsweredAt);
        Assert.NotNull(row.ReplyJson);
    }

    [Fact]
    public async Task ACollect_afterTheConversationMovedOn_setsTheReplyAsideAndRefuses()
    {
        using var harness = new AssistantHarness(inlineWait: TimeSpan.FromMilliseconds(100));
        await SeedProjectsAsync(harness);
        harness.Claude.ReplyDelay = TimeSpan.FromMilliseconds(400);
        harness.Claude.ThenSay("First answer.");

        // Ask, and take only the Pending result — do not collect.
        var conversation = await harness.StartAsync("first question", OnAbbotRoadRfis());
        var first = await harness.RunHopAsync(conversation, OnAbbotRoadRfis());
        Assert.Equal(AiTurnStatus.Pending, first.Status);
        var replyId = first.PendingReplyId!;

        // The user sends again before the answer is collected.
        await harness.AddUserMessageAsync(conversation, "second question");
        await Task.Delay(600);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.CollectAsync(conversation, OnAbbotRoadRfis(), replyId));
        Assert.Contains("moved on", refusal.Message);
        var row = harness.Db.AiPendingReplies.Single(pending => pending.ReplyId == replyId);
        Assert.Equal(AiPendingReplyStatus.Consumed, row.Status);
        // Nothing of the late answer reached the transcript.
        Assert.DoesNotContain(harness.Db.AiConversationMessages.Where(m => m.ConversationId == conversation.ConversationId),
            m => m.Body == "First answer.");
    }

    // ---- 2026-08-25, 14:31–14:33: the V2 build-up staged, then three fresh "the dialog is open
    // beside me" conversations a minute apart, each billed — the model re-opened the dialog it
    // was already sitting in, and every re-open restarted the task. ----

    [Fact]
    public async Task OpenModal_forTheDialogAlreadyOpenWithItsTask_isRefusedTowardsUpdateOpenModal()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 2, VariationOrderStatus.Quoting);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_build_up", record_id = id }))
            .ThenSay("The schedule is already in the dialog.");

        // The kick-off's scope: the build-up dialog for V2 is open and its task is live.
        var scope = new AiScope(AbbotRoad, $"/projects/{AbbotRoad}/variations/{id}", "Variation Orders",
            RecordType: "variation", RecordId: id,
            Task: new AiTaskScope("variation-build-up", "variation_build_up", "Variation", id, "V2", "{}"));

        var turn = await harness.SendAsync("The \"Agreed build-up\" dialog for V2 is open beside me. Stage it.", scope);

        Assert.Empty(turn.UiActions);
        var result = turn.LastToolResult("open_modal");
        Assert.Contains("already open beside the user", result);
        Assert.Contains("update_open_modal", result);
        Assert.Equal(AiTurnStatus.Complete, turn.Status);
    }

    [Fact]
    public async Task OpenModal_forTheSameDialogOnAnotherRecord_stillOpens()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var v2 = await SeedVariationAsync(harness, 2, VariationOrderStatus.Quoting);
        var v3 = await SeedVariationAsync(harness, 3, VariationOrderStatus.Quoting);
        harness.Claude
            .Then(ScriptedClaude.Call("open_modal", new { modal_key = "variation_build_up", record_id = v3 }))
            .ThenSay("V3's build-up dialog is open.");

        var scope = new AiScope(AbbotRoad, $"/projects/{AbbotRoad}/variations/{v2}", "Variation Orders",
            RecordType: "variation", RecordId: v2,
            Task: new AiTaskScope("variation-build-up", "variation_build_up", "Variation", v2, "V2", "{}"));

        var turn = await harness.SendAsync("now do V3", scope);

        var action = Assert.Single(turn.UiActions);
        Assert.Equal("open_modal", action.Tool);
    }

    [Fact]
    public async Task NavigateTo_carryingOpenModal_isRefusedTowardsOpenModal()
    {
        using var harness = new AssistantHarness();
        await SeedProjectsAsync(harness);
        var id = await SeedVariationAsync(harness, 2, VariationOrderStatus.Quoting);
        harness.Claude
            .Then(ScriptedClaude.Call("navigate_to", new { route = $"/projects/{AbbotRoad}/variations/{id}?openModal=variation_build_up", reason = "open the build-up" }))
            .ThenSay("Opening it properly.");

        var turn = await harness.SendAsync("stage V2's build-up", OnAbbotRoadRfis());

        Assert.Empty(turn.UiActions);
        Assert.Contains("navigate_to never opens a dialog", turn.LastToolResult("navigate_to"));
    }
}
