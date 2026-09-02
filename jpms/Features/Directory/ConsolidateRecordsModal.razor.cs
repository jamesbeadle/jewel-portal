using Jewel.JPMS.Contracts.Subcontractors;
using static Jewel.JPMS.Features.Directory.DirectoryDisplay;

namespace Jewel.JPMS.Features.Directory;

public partial class ConsolidateRecordsModal
{
    [Inject] private ISubcontractorStore SubcontractorStore { get; set; } = default!;

    /// <summary>The merge went through — the page clears its ticks.</summary>
    [Parameter] public EventCallback OnMerged { get; set; }

    private bool open;
    private bool consolidating;
    private string? error;
    private string masterId = "";
    private List<Subcontractor> candidates = new();
    private List<MergeField> mergeFields = new();

    private sealed class MergeField
    {
        public string Key = "";
        public string Fieldname = "";
        public List<string> Options = new();
        public string Chosen = "";
    }

    /// <summary>Opens the dialog over the ticked records — two or more, or nothing happens.</summary>
    public void Open(List<Subcontractor> ticked)
    {
        candidates = ticked;
        if (candidates.Count < 2) return;

        // Default master: the oldest record — the company has been known under it the longest.
        masterId = candidates.OrderBy(sub => sub.OnboardedAt).First().SubcontractorId;

        var master = candidates.First(sub => sub.SubcontractorId == masterId);
        mergeFields = new List<MergeField>
        {
            BuildField("company", "Company name", master.CompanyName, candidates.Select(sub => sub.CompanyName)),
            BuildField("category", "Type", Label(master.Category), candidates.Select(sub => Label(sub.Category))),
            BuildField("contact", "Contact name", master.ContactName, candidates.Select(sub => sub.ContactName)),
            BuildField("email", "Email", master.ContactEmail, candidates.Select(sub => sub.ContactEmail)),
            BuildField("phone", "Phone", master.ContactPhone, candidates.Select(sub => sub.ContactPhone)),
            BuildField("mobile", "Mobile", master.MobileNumber, candidates.Select(sub => sub.MobileNumber)),
            BuildField("address", "Address line", master.AddressLine, candidates.Select(sub => sub.AddressLine)),
            BuildField("town", "Town", master.Town, candidates.Select(sub => sub.Town)),
            BuildField("county", "County", master.County, candidates.Select(sub => sub.County)),
            BuildField("postcode", "Postcode", master.Postcode, candidates.Select(sub => sub.Postcode)),
            BuildField("website", "Website", master.Website, candidates.Select(sub => sub.Website)),
            BuildField("cis", "CIS status", master.CisStatus, candidates.Select(sub => sub.CisStatus)),
            BuildField("terms", "Payment terms (days)", master.PaymentTermsDays.ToString(),
                candidates.Select(sub => sub.PaymentTermsDays.ToString()))
        };

        error = null;
        open = true;
        StateHasChanged();
    }

    private static MergeField BuildField(string key, string fieldname, string masterValue, IEnumerable<string> values)
    {
        var options = values
            .Select(value => (value ?? "").Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var chosen = !string.IsNullOrWhiteSpace(masterValue) ? masterValue.Trim() : options.FirstOrDefault() ?? "";
        return new MergeField { Key = key, Fieldname = fieldname, Options = options, Chosen = chosen };
    }

    private string ChosenValue(string key) =>
        mergeFields.FirstOrDefault(field => field.Key == key)?.Chosen ?? "";

    private async Task SaveAsync()
    {
        if (consolidating) return;
        if (string.IsNullOrWhiteSpace(masterId)) { error = "Pick the master record."; return; }
        if (string.IsNullOrWhiteSpace(ChosenValue("company"))) { error = "Company name is required."; return; }
        error = null;

        // The Type radio carries the label; map it back to the enum (labels are one-to-one).
        var category = AllCategories.FirstOrDefault(c => Label(c) == ChosenValue("category"));
        var terms = int.TryParse(ChosenValue("terms"), out var parsedTerms) ? parsedTerms : 30;
        var mergedIds = candidates
            .Select(sub => sub.SubcontractorId)
            .Where(id => !string.Equals(id, masterId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        try
        {
            consolidating = true;
            await SubcontractorStore.ConsolidateAsync(new ConsolidateDirectoryRecords(
                masterId, mergedIds,
                ChosenValue("company"), ChosenValue("contact"), ChosenValue("email"), ChosenValue("phone"),
                ChosenValue("cis"), category, ChosenValue("mobile"), ChosenValue("town"), ChosenValue("county"),
                ChosenValue("website"), terms,
                AddressLine: ChosenValue("address"), Postcode: ChosenValue("postcode")));
            open = false;
            await OnMerged.InvokeAsync();
        }
        catch (CommandFailedException ex) { error = $"Couldn't consolidate: {ex.Message}"; }
        catch { error = "Couldn't consolidate the records. Please try again."; }
        finally { consolidating = false; }
    }
}
