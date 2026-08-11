using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Components;

public partial class DirectoryContactForm : IDisposable
{
    /// <summary>The company as it would be added — the host upserts now (directory page) or
    /// stages it (System Actions).</summary>
    public sealed record Draft(Subcontractor Company);

    /// <summary>Raised by <see cref="TrySubmitAsync"/> when the form validates.</summary>
    [Parameter] public EventCallback<Draft> OnSubmit { get; set; }

    private static readonly DirectoryCategory[] AllCategories =
        (DirectoryCategory[])Enum.GetValues(typeof(DirectoryCategory));

    private string company = "", contact = "", email = "", phone = "", mobile = "";
    private string town = "", county = "", website = "", addressLine = "", postcode = "";
    private string newTrade = "";
    private bool addingTrade;
    private readonly HashSet<string> tradeIds = new(StringComparer.OrdinalIgnoreCase);
    private DirectoryCategory category = DirectoryCategory.Subcontractor;
    private string? error;

    protected override void OnInitialized()
    {
        SubcontractorStore.OnChange += StoreChanged;
        _ = SubcontractorStore.Trades(); // kick off the curated trade list (async; raises OnChange)
    }

    public void Dispose() => SubcontractorStore.OnChange -= StoreChanged;

    private void StoreChanged() => InvokeAsync(StateHasChanged);

    private IReadOnlyList<Trade> SelectedTrades =>
        SubcontractorStore.Trades().Where(t => tradeIds.Contains(t.TradeId)).ToList();

    private void AddTradeToSelection(ChangeEventArgs e)
    {
        var id = e.Value?.ToString();
        if (!string.IsNullOrEmpty(id)) tradeIds.Add(id);
    }

    // Adds a brand-new trade to the curated list and selects it for this company. Immediate by
    // design: the trade is master reference data, useful whether or not the company saves.
    private async Task CreateTrade()
    {
        if (addingTrade || string.IsNullOrWhiteSpace(newTrade)) return;
        error = null;
        try
        {
            addingTrade = true;
            var trade = await SubcontractorStore.AddTradeAsync(newTrade.Trim());
            tradeIds.Add(trade.TradeId);
            newTrade = "";
        }
        catch { error = "Couldn't add that trade. Please try again."; }
        finally { addingTrade = false; }
    }

    /// <summary>Validate and, when valid, hand the draft to the host through OnSubmit.</summary>
    public async Task<bool> TrySubmitAsync()
    {
        error = null;
        if (string.IsNullOrWhiteSpace(company)) { error = "Company name is required."; return false; }
        if ((category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier) && tradeIds.Count == 0)
        { error = "At least one trade is required for subcontractors and suppliers."; return false; }

        await OnSubmit.InvokeAsync(new Draft(new Subcontractor(
            "", company.Trim(), SelectedTrades, contact.Trim(), email.Trim(), phone.Trim(),
            "", DateTimeOffset.UtcNow, category, mobile.Trim(), town.Trim(), county.Trim(), website.Trim(),
            AddressLine: addressLine.Trim(), Postcode: postcode.Trim())));
        return true;
    }

    public void Reset()
    {
        company = contact = email = phone = mobile = town = county = website = addressLine = postcode = newTrade = "";
        tradeIds.Clear();
        category = DirectoryCategory.Subcontractor;
        error = null;
        StateHasChanged();
    }

    public void ShowError(string message)
    {
        error = message;
        StateHasChanged();
    }

    private static string CategoryLabel(DirectoryCategory value) => value switch
    {
        DirectoryCategory.Subcontractor => "Subcontractor",
        DirectoryCategory.Client => "Client",
        DirectoryCategory.Architect => "Architect",
        DirectoryCategory.Supplier => "Supplier",
        _ => "Other"
    };
}
