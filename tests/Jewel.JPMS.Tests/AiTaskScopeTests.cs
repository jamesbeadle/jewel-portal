using System.Text.Json;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The contract between the assistant panel, the server prompt and a task dialog.
//
// ModalCatalog is the registry that decides which dialogs the assistant may fill in at all
// (docs/ai/00-agent-architecture.md §5). Registering one is an explicit grant, so the things worth
// pinning are: that the grant is role-filtered, and that the schema handed to the model actually
// describes the form the browser will merge into. A field name that drifts between the two does not
// fail loudly — the model sends "summary", the dialog ignores it, and the user watches an assistant
// claim to have filled in a form that never changed.
public sealed class AiTaskScopeTests
{
    [Fact]
    public void AiScope_taskIsOptional_soThePlainConstructionSitesKeepWorking()
    {
        // The chat panel builds this shape for every non-task turn, and the server reads Task to
        // decide whether the "you can only read and navigate" rule is in force. If the parameter
        // ever stopped being defaulted, every general conversation would need a null threaded
        // through it — and a required parameter is exactly the kind of thing somebody "fixes" by
        // passing an empty task.
        var scope = new AiScope("proj-1", "/projects/proj-1/requests", "Requests");

        Assert.Null(scope.Task);
    }

    [Fact]
    public void VariationDraft_isRegistered_andOnlyForTheRolesThatCanRaiseAVariation()
    {
        var modal = ModalCatalog.Find("variation_draft");
        Assert.NotNull(modal);

        // Whoever may raise a variation may have the assistant draft it. Anyone else must not even
        // be told the dialog exists.
        Assert.True(ModalCatalog.CanOpen(modal!, new[] { Role.ProjectManager }));
        Assert.True(ModalCatalog.CanOpen(modal!, new[] { Role.QuantitySurveyor }));
        Assert.True(ModalCatalog.CanOpen(modal!, new[] { Role.ManagingDirector }));
        Assert.False(ModalCatalog.CanOpen(modal!, new[] { Role.SiteManager }));
        Assert.False(ModalCatalog.CanOpen(modal!, new[] { Role.Subcontractor }));

        // The Finance Director HAS the assistant but must not have this dialog: the API's
        // CreateVoqFromRfq gate (VariationRoles.AllowedToManageVariations) does not include them, so
        // offering it would end in a 403 on the button after a paid read of the whole email thread.
        Assert.False(ModalCatalog.CanOpen(modal!, new[] { Role.FinanceDirector }));

        // Admin passes every gate in this system (SignedInUserResolver grants them all roles), and
        // this registry must not be the one place that is not true.
        Assert.True(ModalCatalog.CanOpen(modal!, new[] { Role.Admin }));
    }

    [Fact]
    public void Find_isCaseInsensitiveAndSafeOnRubbish()
    {
        // The key arrives from the model on one path and from an untrusted client scope on another.
        Assert.NotNull(ModalCatalog.Find("VARIATION_DRAFT"));
        Assert.Null(ModalCatalog.Find("variation_quote"));
        Assert.Null(ModalCatalog.Find(""));
        Assert.Null(ModalCatalog.Find(null));
    }

    [Fact]
    public void SchemaFor_describesEveryFieldTheDialogMergesBack()
    {
        var json = JsonSerializer.Serialize(ModalCatalog.SchemaFor(ModalCatalog.VariationDraft));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("object", root.GetProperty("type").GetString());

        // These names are the contract with ProjectRequestDetail.SerialiseDraft /
        // ApplyAssistantDraft. Renaming one on either side alone is a silent no-op.
        var properties = root.GetProperty("properties");
        foreach (var field in new[] { "title", "description", "estimatedValue", "trade", "lines" })
        {
            Assert.True(properties.TryGetProperty(field, out var declared), $"missing field: {field}");
            Assert.False(
                string.IsNullOrWhiteSpace(declared.GetProperty("description").GetString()),
                $"{field} has no description — the description IS the model's instruction for it");
        }

        // Title is the only field the dialog refuses to be raised without, so it is the only one
        // the model is told is required.
        var required = root.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToList();
        Assert.Equal(new[] { "title" }, required);
    }

    [Fact]
    public void SchemaFor_givesScopeLinesAnItemShape_includingTheCostCode()
    {
        var json = JsonSerializer.Serialize(ModalCatalog.SchemaFor(ModalCatalog.VariationDraft));
        using var document = JsonDocument.Parse(json);

        var lines = document.RootElement.GetProperty("properties").GetProperty("lines");
        Assert.Equal("array", lines.GetProperty("type").GetString());

        // Without an items schema the model is guessing at the shape of a tender line — and a made-up
        // cost code is a real committed value landing on the wrong cost centre.
        var item = lines.GetProperty("items").GetProperty("properties");
        foreach (var field in new[] { "description", "unit", "quantity", "trade", "costCode" })
            Assert.True(item.TryGetProperty(field, out _), $"missing line field: {field}");
    }

    [Fact]
    public void EveryRegisteredModal_hasAWellFormedRouteTemplate()
    {
        // ChatPanel.ApplyOpenModal substitutes {project} and {record} and navigates to the
        // result. Record-less and project-less dialogs are real shapes (compose_email,
        // worker_week…) — the old both-placeholders assertion predates them and sat red against
        // seven of ten dialogs, masking real regressions. The true invariants:
        foreach (var modal in ModalCatalog.All)
        {
            Assert.StartsWith("/", modal.RouteTemplate);

            // Only placeholders the client knows how to fill.
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(modal.RouteTemplate, "\\{[^}]+\\}"))
            {
                Assert.Contains(token.Value, new[] { "{project}", "{record}" });
            }

            // A record in the PATH needs its project segment to build the route at all.
            var stem = modal.RouteTemplate.Split('?')[0];
            if (stem.Contains("{record}", StringComparison.Ordinal))
                Assert.Contains("{project}", stem);
        }
    }

    [Fact]
    public void ModalKeys_areUniqueSnakeCase()
    {
        var keys = ModalCatalog.All.Select(modal => modal.ModalKey).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var key in keys)
            Assert.Matches("^[a-z][a-z0-9_]*$", key);
    }

    [Fact]
    public void EveryModalRoute_resolvesToAPageGuide()
    {
        // The model is told to read a page's guide before working it — a dialog whose hosting
        // route has no guide strands that instruction. Guides and dialogs must move together.
        foreach (var modal in ModalCatalog.All)
        {
            var stem = modal.RouteTemplate.Split('?')[0];
            Assert.True(PageGuideCatalogue.FindForRoute(stem) is not null,
                $"no page guide covers {modal.ModalKey}'s route {stem}");
        }
    }

    [Fact]
    public void EveryPageGuideRoute_isWellFormed()
    {
        foreach (var guide in PageGuideCatalogue.All)
        {
            Assert.StartsWith("/", guide.RouteTemplate);
            Assert.False(string.IsNullOrWhiteSpace(guide.Guide));
            if (guide.Aliases is null) continue;
            foreach (var alias in guide.Aliases) Assert.StartsWith("/", alias);
        }
    }

    [Fact]
    public void PageGuideRoutes_areUniquePerTemplate()
    {
        var templates = PageGuideCatalogue.All
            .SelectMany(guide => new[] { guide.RouteTemplate }.Concat(guide.Aliases ?? Array.Empty<string>()))
            .Select(route => route.TrimEnd('/'))
            .ToList();
        var duplicates = templates
            .GroupBy(route => route, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        Assert.True(duplicates.Count == 0, "duplicate guide routes: " + string.Join(", ", duplicates));
    }

    // ---- The valuation loop (2026-08-25): variation_edit_lines + claim_progress ----

    [Fact]
    public void VariationEditLines_isRegistered_forExactlyThePageCanManageSet()
    {
        var modal = ModalCatalog.Find("variation_edit_lines");
        Assert.NotNull(modal);
        Assert.Equal("/projects/{project}/variations/{record}", modal!.RouteTemplate);

        // ProjectVariationDetail.CanManage: Admin, MD, PM, QS — who may approve and revise.
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.ProjectManager }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.QuantitySurveyor }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.ManagingDirector }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.Admin }));
        Assert.False(ModalCatalog.CanOpen(modal, new[] { Role.FinanceDirector }));
        Assert.False(ModalCatalog.CanOpen(modal, new[] { Role.SiteManager }));
        Assert.False(ModalCatalog.CanOpen(modal, new[] { Role.Subcontractor }));
    }

    [Fact]
    public void VariationEditLines_schemaMatchesTheDialogsSnapshot()
    {
        // VariationApprovePanel.SnapshotLines emits { lines: [{valuationLineItemId, costCode,
        // description, quantity, rate, amount}] } and ReplaceLines reads the same names back.
        // A name that drifts is a line the model sends and the panel ignores.
        var json = JsonSerializer.Serialize(ModalCatalog.SchemaFor(ModalCatalog.VariationEditLines));
        using var document = JsonDocument.Parse(json);

        var lines = document.RootElement.GetProperty("properties").GetProperty("lines");
        Assert.Equal("array", lines.GetProperty("type").GetString());
        var item = lines.GetProperty("items").GetProperty("properties");
        foreach (var field in new[] { "valuationLineItemId", "costCode", "description", "quantity", "rate" })
            Assert.True(item.TryGetProperty(field, out _), $"missing line field: {field}");
        Assert.Equal("number", item.GetProperty("quantity").GetProperty("type").GetString());
        Assert.Equal("number", item.GetProperty("rate").GetProperty("type").GetString());

        var required = document.RootElement.GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToList();
        Assert.Contains("lines", required);
    }

    [Fact]
    public void ClaimProgress_isRegistered_forExactlyTheClaimEntryGate()
    {
        var modal = ModalCatalog.Find("claim_progress");
        Assert.NotNull(modal);
        Assert.Equal("/projects/{project}/valuation", modal!.RouteTemplate);

        // ValuationReportAuthorisation.RolesThatMayRecordClaimEntries: Director, FD, PM, QS.
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.ManagingDirector }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.FinanceDirector }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.ProjectManager }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.QuantitySurveyor }));
        Assert.True(ModalCatalog.CanOpen(modal, new[] { Role.Admin }));
        Assert.False(ModalCatalog.CanOpen(modal, new[] { Role.SiteManager }));
        Assert.False(ModalCatalog.CanOpen(modal, new[] { Role.Architect }));
    }

    [Fact]
    public void ClaimProgress_schemaMatchesTheDialogsState()
    {
        // ClaimProgressDialog.SerialiseState emits { claim, entries: [{valuationLineItemId, line,
        // currentPercent, percentComplete}] } and HandleAssistantDraft reads entries back by the
        // same two names the schema declares.
        var json = JsonSerializer.Serialize(ModalCatalog.SchemaFor(ModalCatalog.ClaimProgress));
        using var document = JsonDocument.Parse(json);

        var entries = document.RootElement.GetProperty("properties").GetProperty("entries");
        Assert.Equal("array", entries.GetProperty("type").GetString());
        var item = entries.GetProperty("items").GetProperty("properties");
        Assert.True(item.TryGetProperty("valuationLineItemId", out _));
        Assert.True(item.TryGetProperty("percentComplete", out _));
        Assert.Equal("number", item.GetProperty("percentComplete").GetProperty("type").GetString());
        var itemRequired = entries.GetProperty("items").GetProperty("required").EnumerateArray().Select(value => value.GetString()).ToList();
        Assert.Contains("valuationLineItemId", itemRequired);
        Assert.Contains("percentComplete", itemRequired);
    }

    [Fact]
    public void VariationBuildUp_isRegistered_forThePreApprovalSide_withTheSameRolesAsEditLines()
    {
        var modal = ModalCatalog.Find("variation_build_up");
        Assert.NotNull(modal);
        Assert.Equal("/projects/{project}/variations/{record}", modal!.RouteTemplate);
        Assert.Equal(ModalCatalog.VariationEditLines.OpenableBy, modal.OpenableBy);

        // VariationApprovePanel.SnapshotLines emits lines[] of {costCode, description, quantity,
        // rate}; the page adds commercialBasis / programmeImpact / exclusions. Same names here.
        var json = JsonSerializer.Serialize(ModalCatalog.SchemaFor(modal));
        using var document = JsonDocument.Parse(json);
        var properties = document.RootElement.GetProperty("properties");
        foreach (var field in new[] { "lines", "commercialBasis", "programmeImpact", "exclusions" })
            Assert.True(properties.TryGetProperty(field, out _), $"missing field: {field}");
        var item = properties.GetProperty("lines").GetProperty("items").GetProperty("properties");
        foreach (var field in new[] { "costCode", "description", "quantity", "rate" })
            Assert.True(item.TryGetProperty(field, out _), $"missing line field: {field}");
    }
}
