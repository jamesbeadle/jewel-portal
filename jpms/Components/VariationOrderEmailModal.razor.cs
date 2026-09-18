using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Components;

public partial class VariationOrderEmailModal
{
    /// <summary>The variation to email; null keeps the modal closed.</summary>
    [Parameter] public VariationOrder? Order { get; set; }

    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Raised once the email has gone or been staged, so the host can refresh the
    /// variation's correspondence without knowing how it was sent.</summary>
    [Parameter] public EventCallback<VariationOrderEmailOutcome> OnSent { get; set; }

    private string recipientOverride = "";
    private bool busy;
    private string? error;
    private VariationOrderEmailOutcome? outcome;

    private Task Send() => DispatchAsync(saveAsDraftOnly: false);

    private Task SaveAsDraft() => DispatchAsync(saveAsDraftOnly: true);

    private async Task DispatchAsync(bool saveAsDraftOnly)
    {
        if (busy || Order is not { } order) return;
        error = null;
        try
        {
            busy = true;
            outcome = await Commands.SendAsync(
                new SendVariationOrderEmail(
                    order.VariationOrderId,
                    string.IsNullOrWhiteSpace(recipientOverride) ? null : recipientOverride.Trim(),
                    saveAsDraftOnly),
                CancellationToken.None);
            await OnSent.InvokeAsync(outcome);
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't email the variation order. Check the project has a client or architect contact with an email address, then try again."; }
        finally { busy = false; }
    }

    private async Task Close()
    {
        outcome = null;
        error = null;
        recipientOverride = "";
        preview = null;
        previewError = null;
        previewedOrderId = null;
        await OnClose.InvokeAsync();
    }
}
