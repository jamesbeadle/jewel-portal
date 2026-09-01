using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- The invite composer (TenderInviteComposerModal owns the compose, the persisted
    // draft and the send itself); each send's outcome lands here for the page's banner. ----

    private TenderInviteComposerModal? inviteComposer;
    private string? sendNote;
    private string? draftWebLink;

    private Task OpenComposeModal() =>
        inviteComposer?.OpenAsync() ?? Task.CompletedTask;

    private void OnInviteSent(BidPackageInviteSendOutcome outcome)
    {
        package = outcome.Package;
        draftWebLink = outcome.WebLink;

        if (outcome.Sent)
        {
            sendNote = $"Invite sent from the projects mailbox to {outcome.RecipientCount} recipient{(outcome.RecipientCount == 1 ? "" : "s")}, tagged {package.Reference} — replies land under Tender responses.";
            if (outcome.LinkedFiles is { Count: > 0 } linked)
            {
                sendNote += linked.Count == 1
                    ? $" 1 file was too large to attach and travels as a 7-day download link: {linked[0]}."
                    : $" {linked.Count} files were too large to attach and travel as 7-day download links: {string.Join(", ", linked)}.";
            }
            // The sent copy appears in the Emails tab as the mailbox catches up.
            _ = ReloadEmailsAsync();
        }
        else
        {
            // Staged but not sent — the email survives in Drafts; say so where the user is.
            sendNote = outcome.FailureNote ?? "The send didn't go through — the invite is saved as a draft in the projects mailbox.";
        }
    }
}
