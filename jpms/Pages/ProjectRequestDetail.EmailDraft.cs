using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;


namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    // Opens the email modal with a clean slate; the tagged chains load in while it's open
    // (the fresh-email option is available immediately).
    private async Task OpenEmailModal()
    {
        if (record is null || busy || preparingDraft || !CanDraftEmail) return;
        draftError = null;
        draftResult = null;
        selectedChainMailboxId = "";
        emailModalOpen = true;
        await LoadTaggedEmailsAsync();
    }

    private void CloseEmailModal()
    {
        if (preparingDraft) return;
        emailModalOpen = false;
    }

    // One confirm for both paths: no chain selected stages a fresh email (recipients from the
    // correspondence profile); a selected chain stages a reply-all draft in that email's thread
    // with the whole conversation quoted beneath. The PDF is attached either way.
    private async Task ConfirmEmailDraft()
    {
        if (record is null || busy || preparingDraft || !CanDraftEmail) return;
        draftError = null;
        draftResult = null;
        try
        {
            preparingDraft = true;
            draftResult = string.IsNullOrEmpty(selectedChainMailboxId)
                ? await RequestRegister.PrepareEmailDraftAsync(record.RequestId)
                : await RequestRegister.PrepareReplyDraftAsync(record.RequestId, selectedChainMailboxId);
            // Drafting the official document moves an Open request to Awaiting Response server-side
            // (manually set back to Open if the send is cancelled) — reload so the status pill agrees.
            await LoadAsync();
        }
        catch (CommandFailedException ex)
        {
            draftError = ex.Message;
        }
        catch
        {
            draftError = string.IsNullOrEmpty(selectedChainMailboxId)
                ? "Couldn't create the draft. Please try again."
                : "Couldn't create the reply draft. The email may no longer be in the mailbox — refresh and try again.";
        }
        finally
        {
            preparingDraft = false;
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;



}
