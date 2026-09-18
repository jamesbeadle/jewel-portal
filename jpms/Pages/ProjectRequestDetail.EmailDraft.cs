using Jewel.JPMS.Contracts.MailboxCompose;
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
        await LoadEmailPreviewAsync();
        await LoadTaggedEmailsAsync();
    }

    private void CloseEmailModal()
    {
        if (preparingDraft) return;
        emailModalOpen = false;
        emailPreview = null;
        emailPreviewError = null;
    }

    // One door for both paths: no chain selected emails the document as a fresh thread (recipients
    // from the correspondence profile); a selected chain replies into that email's thread with the
    // whole conversation quoted beneath. The PDF is attached either way, and saveAsDraftOnly leaves
    // the reviewed email in the mailbox's Drafts folder instead of sending it.
    private async Task EmailTheDocument(bool saveAsDraftOnly)
    {
        if (record is null || busy || preparingDraft || !CanDraftEmail) return;
        draftError = null;
        draftResult = null;
        try
        {
            preparingDraft = true;
            draftResult = string.IsNullOrEmpty(selectedChainMailboxId)
                ? await RequestRegister.EmailDocumentAsync(record.RequestId, saveAsDraftOnly: saveAsDraftOnly)
                : await RequestRegister.EmailDocumentReplyAsync(record.RequestId, selectedChainMailboxId, saveAsDraftOnly);
            // Emailing the official document moves an Open request to Awaiting Response server-side
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
                ? "Couldn't email the document. Please try again."
                : "Couldn't email the reply. The original email may no longer be in the mailbox — refresh and try again.";
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
