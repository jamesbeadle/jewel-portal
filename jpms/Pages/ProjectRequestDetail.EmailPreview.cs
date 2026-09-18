using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    /// <summary>The words nobody could read before 2026-09-18: SendRequestEmail takes no subject
    /// and no body, so the cover note is composed server-side and the modal showed nothing. This
    /// asks the server for its own composition rather than building a second one here.</summary>
    private async Task LoadEmailPreviewAsync()
    {
        if (record is null) return;
        emailPreview = null;
        emailPreviewError = null;
        emailPreviewLoading = true;
        try
        {
            emailPreview = await Queries.AskAsync(
                new PreviewRecordEmail(RecordType.Request, record.RequestId),
                CancellationToken.None);
        }
        catch
        {
            emailPreviewError =
                "Couldn't read the email yet — check the request or its project has a client or "
                + "architect contact with an email address.";
        }
        finally
        {
            emailPreviewLoading = false;
            StateHasChanged();
        }
    }
}
