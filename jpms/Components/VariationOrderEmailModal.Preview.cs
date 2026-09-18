using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Components;

public partial class VariationOrderEmailModal
{
    private string? previewedOrderId;
    private bool previewing;
    private string? previewError;
    private RecordEmailPreview? preview;

    private async Task OnOverrideChanged(ChangeEventArgs args)
    {
        recipientOverride = args.Value?.ToString() ?? "";
        await LoadPreviewAsync();
    }

    /// <summary>The modal shows the server's own composition rather than a copy of the note built
    /// here, because two spellings of one email is what put the project name in the subject
    /// twice.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (Order is not { } order) { preview = null; previewedOrderId = null; return; }
        if (previewedOrderId == order.VariationOrderId) return;
        previewedOrderId = order.VariationOrderId;
        await LoadPreviewAsync();
    }

    private async Task LoadPreviewAsync()
    {
        if (Order is not { } order) return;
        previewError = null;
        previewing = true;
        try
        {
            preview = await Queries.AskAsync(
                new PreviewRecordEmail(
                    RecordType.Variation,
                    order.VariationOrderId,
                    string.IsNullOrWhiteSpace(recipientOverride) ? null : recipientOverride.Trim()),
                CancellationToken.None);
        }
        catch
        {
            preview = null;
            previewError = "Couldn't read the email yet — check the project has a client or architect contact with an email address.";
        }
        finally { previewing = false; }
    }
}
