using Jewel.JPMS.Features.Procurement;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Package details: summary + line schedule, one dialog (PackageDetailsEditorModal
    // owns the drafts and the cost-code rule). Its one Save lands as two commands here. ----

    private bool showDetailsModal;

    private void EditDetails()
    {
        if (!CanEdit || package is null) return;
        error = null;
        showDetailsModal = true;
    }

    private void CancelDetails()
    {
        if (busy) return;
        showDetailsModal = false;
    }

    private async Task SaveDetails(BidPackageDetailsDraft draft)
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;

            // Summary first, then the schedule — two commands behind one Save. If the second
            // fails the first has still landed; the catch says so and the dialog stays open with
            // everything the user typed.
            package = await Commands.SendAsync(
                new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status,
                    package.OwnerEmail, package.MaterialsApplicable, draft.SpecificationSummary),
                CancellationToken.None);

            fetchedLineItems = await Commands.SendAsync(
                new SetBidPackageLineItems(BidPackageId, draft.LineItems.ToList()), CancellationToken.None);

            showDetailsModal = false;
        }
        catch { error = "Couldn't save the package details — check what's on the record before retrying, the summary may have saved without the lines."; }
        finally { busy = false; }
    }
}
