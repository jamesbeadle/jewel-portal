using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Record a tender submission, review, save as a quote ----
    // The modal (TenderSubmissionModal) owns the whole review flow — manual keying and the
    // AI extract-from-email path; the page only points it at the package and reloads on save.

    private TenderSubmissionModal? tenderSubmissionModal;
    private string? awardNote;

    private void OpenManualTenderModal() => tenderSubmissionModal?.OpenManual();

    private Task OpenExtractFromEmail(MailboxMessage email) =>
        tenderSubmissionModal?.OpenFromEmailAsync(email) ?? Task.CompletedTask;
}
