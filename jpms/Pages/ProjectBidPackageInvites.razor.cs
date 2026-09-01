using Jewel.JPMS.Services.Excel;
using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInvites
{
    [Parameter] public string ProjectId { get; set; } = "";

    private bool isLoaded;
    private bool busy;
    private string? error;
    private bool showCreateModal;
    private string createTitle = "";
    private string createTrade = "";
    private bool createMaterials;

    // ---- Suggest bid packages (AI) ----
    private bool showSuggestModal;
    private bool suggestBusy;
    private bool createBusy;
    private string? suggestError;
    private string suggestModelKey = AiModelCatalogue.DefaultKey;   // cheap by default, on purpose
    private int suggestHops;
    private string suggestModelUsed = "";
    private string? suggestNote;
    private IReadOnlyList<BidPackageSuggestion>? suggestions;        // null = not run yet
    private readonly HashSet<int> selectedSuggestions = new();

    // Closed packages are ordered last, not hidden — the record stays reachable (the same rule
    // as completed projects). OrderBy is stable, so the store's newest-first order holds within
    // each band.
    private IReadOnlyList<BidPackage> Packages => ProcurementStore.PackagesFor(ProjectId)
        .OrderBy(p => p.Status == BidPackageStatus.Closed ? 1 : 0)
        .ToList();

    private bool CanManage => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    private void OpenCreateModal()
    {
        createTitle = "";
        createTrade = "";
        createMaterials = false;
        error = null;
        showCreateModal = true;
    }

    private void CloseCreateModal() => showCreateModal = false;

    // Creates the Draft package then goes straight to its detail page, where line items,
    // drawings and recipients are managed.
    private async Task ConfirmCreate()
    {
        if (busy || !CanManage || string.IsNullOrWhiteSpace(createTitle) || string.IsNullOrWhiteSpace(createTrade)) return;
        error = null;
        try
        {
            busy = true;
            var package = await Commands.SendAsync(
                new CreateBidPackage(ProjectId, createTitle.Trim(), createTrade.Trim(),
                    Auth.CurrentUser?.Email ?? "", createMaterials), CancellationToken.None);
            showCreateModal = false;
            ProcurementStore.Refresh(ProjectId);
            Nav.NavigateTo($"/projects/{ProjectId}/bid-package-invites/{package.BidPackageId}");
        }
        catch { error = "Couldn't create the bid package. Please try again."; }
        finally { busy = false; }
    }

    // ---- Suggest bid packages (AI) ------------------------------------------------------------

    private int SelectedCount => selectedSuggestions.Count;

    private string SuggestModelHint =>
        AiModelCatalogue.Find(suggestModelKey)?.Hint ?? "Which Claude model analyses the report";

    private void OpenSuggestModal()
    {
        suggestError = null;
        suggestions = null;
        suggestNote = null;
        selectedSuggestions.Clear();
        showSuggestModal = true;
    }

    private void CloseSuggestModal()
    {
        if (createBusy) return;   // mid-create, closing would hide work still landing
        showSuggestModal = false;
    }

    private void ToggleSuggestion(int index, bool selected)
    {
        if (selected) selectedSuggestions.Add(index);
        else selectedSuggestions.Remove(index);
    }

    // One request = one bounded Claude chunk (the SWA gateway kills anything near 45s, which is
    // what a whole Fable answer in one call hit). An incomplete result hands back PartialText
    // and we immediately re-send it so the model continues mid-answer; the hop cap is a backstop
    // against a model that never says it's finished.
    private const int MaxSuggestionHops = 12;

    private async Task RunSuggestion()
    {
        if (suggestBusy || !CanManage) return;
        suggestError = null;
        suggestions = null;
        suggestNote = null;
        selectedSuggestions.Clear();
        try
        {
            suggestBusy = true;
            suggestHops = 0;
            var partial = "";
            BidPackageSuggestionResult result;
            do
            {
                suggestHops++;
                StateHasChanged();
                result = await Commands.SendAsync(
                    new SuggestBidPackages(ProjectId, suggestModelKey, partial), CancellationToken.None);
                partial = result.PartialText;
            } while (!result.IsComplete && suggestHops < MaxSuggestionHops);

            if (!result.IsComplete)
            {
                suggestError = "The answer never finished — try again, or pick a different model.";
                return;
            }

            suggestions = result.Suggestions;
            suggestModelUsed = result.ModelUsed;
            suggestNote = result.Note;
            // Everything starts ticked — unticking is the review act.
            for (var i = 0; i < result.Suggestions.Count; i++) selectedSuggestions.Add(i);
        }
        catch { suggestError = "Couldn't get suggestions. Please try again."; }
        finally { suggestBusy = false; }
    }

    // Each ticked suggestion becomes an ordinary Draft package (CreateBidPackage), with the
    // suggested scope written on as the package's "what this covers" summary. Stops at the
    // first failure so a partial batch is visible rather than silently incomplete.
    private async Task CreateSelectedSuggestions()
    {
        if (createBusy || !CanManage || suggestions is null || selectedSuggestions.Count == 0) return;
        suggestError = null;
        try
        {
            createBusy = true;
            foreach (var index in selectedSuggestions.OrderBy(i => i))
            {
                if (index >= suggestions.Count) continue;
                var suggestion = suggestions[index];
                var package = await Commands.SendAsync(
                    new CreateBidPackage(ProjectId, suggestion.Title, suggestion.Trade,
                        Auth.CurrentUser?.Email ?? "", suggestion.MaterialsApplicable), CancellationToken.None);
                if (!string.IsNullOrWhiteSpace(suggestion.Scope))
                {
                    await Commands.SendAsync(
                        new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade,
                            package.Status, package.OwnerEmail, package.MaterialsApplicable,
                            suggestion.Scope), CancellationToken.None);
                }
            }
            showSuggestModal = false;
            ProcurementStore.Refresh(ProjectId);
        }
        catch { suggestError = "Couldn't create all the selected packages — the ones already created are in the list below. Please review and try again."; }
        finally { createBusy = false; }
    }

    private ExcelWorkbook? BuildExportWorkbook(bool _)
    {
        var packages = Packages;
        if (packages.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Bid package invites",
            new ExcelColumn("Package"),
            new ExcelColumn("Trade"),
            new ExcelColumn("Status"),
            new ExcelColumn("Created", ExcelFormat.Date));

        foreach (var package in packages)
        {
            sheet.AddRow(
                package.Title,
                package.Trade,
                package.Status.ToString(),
                package.CreatedAt.LocalDateTime);
        }
        return workbook;
    }

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        ProcurementStore.OnChange += StateHasChanged;
        SubcontractorStore.OnChange += StateHasChanged;
        // Refresh on entry: cached packages render immediately, then update when the
        // background reload lands — so navigating back to this tab never shows stale data.
        ProcurementStore.Refresh(ProjectId);
        _ = SubcontractorStore.Trades(); // the create modal's trade dropdown (async; raises OnChange)
        isLoaded = true;
    }

    public void Dispose()
    {
        ProcurementStore.OnChange -= StateHasChanged;
        SubcontractorStore.OnChange -= StateHasChanged;
    }
}
