using Jewel.JPMS.Commercial;
using Jewel.JPMS.Features.Commercial;

namespace Jewel.JPMS.Pages;

public partial class ProjectValuation
{

    // ---- Set % complete: the dialog ------------------------------------------------------------

    private bool claimProgressOpen;

    private void OpenClaimProgress()
    {
        if (Selected is not { Status: ValuationClaimStatus.Draft }) return;
        claimProgressOpen = true;
    }

    private void CloseClaimProgress()
    {
        claimProgressOpen = false;
    }

    private void ClaimProgressSaved()
    {
        claimProgressOpen = false;
        // The store re-fetched the claim's entries and the claims (totals re-frozen) on save.
    }
}
