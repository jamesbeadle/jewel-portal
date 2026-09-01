using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Pages;

public partial class Subcontractors
{
    // ---- Import from Xero --------------------------------------------------
    // The modal lists Xero's suppliers (contacts that have had a bill). Its search box starts
    // from whatever is in the directory search box, so "sear" over the directory carries straight
    // into the Xero list. Importing creates a NEW linked record every time — duplicates are then
    // resolved with Consolidate below, one consistent flow for all duplicates.

    private bool importOpen;
    private string importSearch = "";
    // Customer-only Xero contacts (clients) are noise in a supplier import; hidden unless asked
    // for. Contacts with NEITHER flag (created in Xero but never yet billed) always show — that
    // is exactly the just-set-up supplier the import exists for.
    private bool importIncludeCustomers;
    private XeroSuppliersSnapshot? importSnapshot;
    private bool importLoading;
    private string? importError;
    private string? importingContactId;

    private void OpenImportModal()
    {
        importSearch = search; // the modal's search starts as the directory's search text
        importError = null;
        importOpen = true;
        if (importSnapshot is null) _ = LoadSuppliersAsync(force: false);
    }

    private void CloseImportModal() => importOpen = false;

    private void OnImportSearchInput(ChangeEventArgs e) => importSearch = e.Value?.ToString() ?? "";

    private async Task LoadSuppliersAsync(bool force)
    {
        if (importLoading) return;
        importError = null;
        try
        {
            importLoading = true;
            if (force) importSnapshot = null;
            importSnapshot = await SubcontractorStore.FetchXeroSuppliersAsync(force);
        }
        catch { importError = "The Xero supplier list couldn't be loaded. Please try again."; }
        finally { importLoading = false; StateHasChanged(); }
    }

    private IReadOnlyList<XeroSupplier> FilteredSuppliers()
    {
        if (importSnapshot is null) return Array.Empty<XeroSupplier>();
        var q = (importSearch ?? "").Trim();
        return importSnapshot.Suppliers
            .Where(supplier => importIncludeCustomers || supplier.IsSupplier || !supplier.IsCustomer)
            .Where(supplier => q.Length == 0
                || supplier.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.EmailAddress.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.Town.Contains(q, StringComparison.OrdinalIgnoreCase)
                || supplier.County.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task ImportSupplierAsync(string contactId)
    {
        if (importingContactId is not null) return;
        importError = null;
        try
        {
            importingContactId = contactId;
            await SubcontractorStore.ImportFromXeroAsync(contactId);
            // Mark the row imported in place rather than re-reading Xero for a flag we already know.
            if (importSnapshot is not null)
            {
                importSnapshot = importSnapshot with
                {
                    Suppliers = importSnapshot.Suppliers
                        .Select(supplier => supplier.ContactId == contactId
                            ? supplier with { AlreadyImported = true }
                            : supplier)
                        .ToList()
                };
            }
        }
        catch (CommandFailedException ex) { importError = $"Couldn't import: {ex.Message}"; }
        catch { importError = "Couldn't import that supplier. Please try again."; }
        finally { importingContactId = null; }
    }

}
