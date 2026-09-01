using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class Subcontractors
{
    // ---- Consolidation -----------------------------------------------------
    // Tick 2+ records, choose the master and the winning value for each detail; the server
    // re-points every reference to the master, keeps losing contact details as contacts, moves
    // the Xero links across and deletes the merged-away records.

    private readonly HashSet<string> selectedIds = new(StringComparer.OrdinalIgnoreCase);
    private bool consolidateOpen;
    private bool consolidating;
    private string? consolidateError;
    private string masterId = "";
    private List<Subcontractor> consolidateCandidates = new();
    private List<MergeField> mergeFields = new();

    private sealed class MergeField
    {
        public string Key = "";
        public string Fieldname = "";
        public List<string> Options = new();
        public string Chosen = "";
    }

    private void ToggleSelected(string subcontractorId, ChangeEventArgs e)
    {
        if (e.Value is bool ticked && ticked) selectedIds.Add(subcontractorId);
        else selectedIds.Remove(subcontractorId);
    }

    // Selection intersected with the current directory, so a stale tick (record merged away in
    // another tab) can never enter a consolidation.
    private List<Subcontractor> SelectedForConsolidation() =>
        SubcontractorStore.All().Where(sub => selectedIds.Contains(sub.SubcontractorId)).ToList();

    private void OpenConsolidateModal()
    {
        consolidateCandidates = SelectedForConsolidation();
        if (consolidateCandidates.Count < 2) return;

        // Default master: the oldest record — the company has been known under it the longest.
        masterId = consolidateCandidates.OrderBy(sub => sub.OnboardedAt).First().SubcontractorId;

        var master = consolidateCandidates.First(sub => sub.SubcontractorId == masterId);
        mergeFields = new List<MergeField>
        {
            BuildField("company", "Company name", master.CompanyName, consolidateCandidates.Select(sub => sub.CompanyName)),
            BuildField("category", "Type", Label(master.Category), consolidateCandidates.Select(sub => Label(sub.Category))),
            BuildField("contact", "Contact name", master.ContactName, consolidateCandidates.Select(sub => sub.ContactName)),
            BuildField("email", "Email", master.ContactEmail, consolidateCandidates.Select(sub => sub.ContactEmail)),
            BuildField("phone", "Phone", master.ContactPhone, consolidateCandidates.Select(sub => sub.ContactPhone)),
            BuildField("mobile", "Mobile", master.MobileNumber, consolidateCandidates.Select(sub => sub.MobileNumber)),
            BuildField("address", "Address line", master.AddressLine, consolidateCandidates.Select(sub => sub.AddressLine)),
            BuildField("town", "Town", master.Town, consolidateCandidates.Select(sub => sub.Town)),
            BuildField("county", "County", master.County, consolidateCandidates.Select(sub => sub.County)),
            BuildField("postcode", "Postcode", master.Postcode, consolidateCandidates.Select(sub => sub.Postcode)),
            BuildField("website", "Website", master.Website, consolidateCandidates.Select(sub => sub.Website)),
            BuildField("cis", "CIS status", master.CisStatus, consolidateCandidates.Select(sub => sub.CisStatus)),
            BuildField("terms", "Payment terms (days)", master.PaymentTermsDays.ToString(),
                consolidateCandidates.Select(sub => sub.PaymentTermsDays.ToString()))
        };

        consolidateError = null;
        consolidateOpen = true;
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

    private void CloseConsolidateModal() => consolidateOpen = false;

    private string ChosenValue(string key) =>
        mergeFields.FirstOrDefault(field => field.Key == key)?.Chosen ?? "";

    private async Task SaveConsolidationAsync()
    {
        if (consolidating) return;
        if (string.IsNullOrWhiteSpace(masterId)) { consolidateError = "Pick the master record."; return; }
        if (string.IsNullOrWhiteSpace(ChosenValue("company"))) { consolidateError = "Company name is required."; return; }
        consolidateError = null;

        // The Type radio carries the label; map it back to the enum (labels are one-to-one).
        var category = AllCategories.FirstOrDefault(c => Label(c) == ChosenValue("category"));
        var terms = int.TryParse(ChosenValue("terms"), out var parsedTerms) ? parsedTerms : 30;
        var mergedIds = consolidateCandidates
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
            selectedIds.Clear();
            consolidateOpen = false;
        }
        catch (CommandFailedException ex) { consolidateError = $"Couldn't consolidate: {ex.Message}"; }
        catch { consolidateError = "Couldn't consolidate the records. Please try again."; }
        finally { consolidating = false; }
    }

    // The screen's "Primary contact" cell is compound (name + email subtitle) — split
    // into two columns so the email isn't lost from the export.
    private ExcelWorkbook? BuildExportWorkbook(bool ignoreFilters)
    {
        // "Ignore search & filter" (offered while either is narrowing the table) exports the
        // complete directory in the same company-name order.
        var subs = ignoreFilters
            ? DirectoryCompanies().OrderBy(s => s.CompanyName).ToList()
            : Filtered();
        if (subs.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Directory",
            new ExcelColumn("Company"),
            new ExcelColumn("Type"),
            new ExcelColumn("Trade"),
            new ExcelColumn("Primary contact"),
            new ExcelColumn("Contact email"),
            new ExcelColumn("Location"));

        foreach (var sub in subs)
        {
            sheet.AddRow(
                sub.CompanyName,
                Label(sub.Category),
                sub.TradesLabel,
                sub.ContactName,
                sub.ContactEmail,
                Location(sub));
        }
        return workbook;
    }

    private static string Location(Subcontractor s) =>
        string.Join(", ", new[] { s.Town, s.County }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string Label(DirectoryCategory c) => c switch
    {
        DirectoryCategory.Subcontractor => "Subcontractor",
        DirectoryCategory.Client => "Client",
        DirectoryCategory.Architect => "Architect",
        DirectoryCategory.Supplier => "Supplier",
        _ => "Other"
    };
}
