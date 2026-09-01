using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Commercial;
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

    // ---- Raise & send the invoice from the claim ---------------------------
    // One click, two moves: creates the invoice for the claim's payment due (first day of the
    // claim date's month as its period; the raise freezes the report snapshot) and sends the
    // claim — straight to awaiting approval. The FD's flow then runs off this card: record
    // approval → issue → record payment. If the send half fails, the invoice sits as a draft
    // and the card's primary button becomes "Send claim", so recovery is the same click.
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
            var invoice = await Invoices.CreateAsync(ProjectId, period, amount, claim.ValuationClaimId);
            try
            {
                await Invoices.SubmitAsync(invoice.ValuationInvoiceId);
            }
            finally
            {
                // Reload even when the send half fails — the raise already happened, and the
                // card must show the draft it left behind. The raise froze a snapshot, so the
                // register refreshes too.
                await ReloadInvoicePanelsAsync();
                OnCertifiedChanged();
            }
        }, "Couldn't raise & send the invoice — if it now shows as drafted, Send claim finishes the job. The server may be restarting.");
    }
}
