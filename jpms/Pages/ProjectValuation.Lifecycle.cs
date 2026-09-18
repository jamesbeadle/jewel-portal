using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;

namespace Jewel.JPMS.Pages;

public partial class ProjectValuation
{

    // ---- Rename / delete ----------------------------------------------------
    private void OpenRename()
    {
        if (Selected is null) return;
        renameValue = Selected.Name;
        showRename = true;
    }

    private Task SaveRenameAsync() => Selected is null || busy ? Task.CompletedTask : GuardAsync(async () =>
    {
        await Store.RenameClaimAsync(ProjectId, Selected!.ValuationClaimId, renameValue.Trim());
        showRename = false;
    }, "Couldn't rename the claim — the server may be restarting. Please try again.");

    private Task DeleteClaimAsync() => Selected is null || busy ? Task.CompletedTask : GuardAsync(async () =>
    {
        await Store.DeleteClaimAsync(ProjectId, Selected!.ValuationClaimId);
        selectedClaimId = "";        // OnStoreChanged re-picks the first remaining claim
        showDeleteClaim = false;
        // Any invoice that pointed at this claim had its link cleared server-side.
        if (invoicesSection is not null) await invoicesSection.ReloadAsync();
    }, "Couldn't delete the claim — the server may be restarting. Please try again.");

    // ---- Raise the invoice from the claim ----------------------------------
    // The handover point: the project team has valued and locked the claim; accounts pick it
    // up here. One move only — creates the invoice for the claim's payment due (first day of
    // the claim date's month as its period) as a DRAFT against the claim's locked statement —
    // the client-facing form of the report. Nothing leaves the portal. Sending the claim to
    // the architect/client happens outside (or via the card's Email statement), and the
    // card's next primary button, "Record claim sent", records that it went — so raising and
    // claiming are two clicks that match two real-world moments. (Until 2026-09-07 this was
    // one click that also marked the claim Submitted, worded "Raise & send invoice" — read as
    // the portal emailing the invoice, which it never did.)
    private Task RaiseInvoiceAsync()
    {
        if (Selected is null || busy) return Task.CompletedTask;
        var amount = PaymentDueNow;
        if (amount <= 0m) return Task.CompletedTask;
        var claim = Selected;
        return GuardAsync(async () =>
        {
            var period = new DateTimeOffset(
                new DateTime(claim.ClaimDate.Year, claim.ClaimDate.Month, 1), TimeSpan.Zero);
            await Invoices.CreateAsync(ProjectId, period, amount, claim.ValuationClaimId);
            await ReloadInvoicePanelsAsync();
            OnCertifiedChanged();
        }, "Couldn't raise the invoice — the server may be restarting. Please try again.");
    }
}
