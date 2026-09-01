using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // ---- Agreed build-up: open / close / stage ----

    private void SeedBuildUpNarratives()
    {
        buildUpCommercialBasis = order?.CommercialBasis ?? "";
        buildUpProgrammeImpact = order?.ProgrammeImpact ?? "";
        buildUpExclusions = order?.Exclusions ?? "";
        buildUpNarrativesOpen = !string.IsNullOrWhiteSpace(buildUpCommercialBasis)
                                || !string.IsNullOrWhiteSpace(buildUpProgrammeImpact)
                                || !string.IsNullOrWhiteSpace(buildUpExclusions);
    }

    private void OpenBuildUp()
    {
        buildUpError = null;
        SeedBuildUpNarratives();
        buildUpModalOpen = true;
    }

    private void CloseBuildUp()
    {
        buildUpModalOpen = false;
        buildUpError = null;
    }

    private async Task StageBuildUp(VariationApprovePanel.ApproveRequest request)
    {
        if (busy || order is null) return;
        buildUpError = null;
        try
        {
            busy = true;
            order = await Variations.StageBuildUpAsync(
                VariationOrderId, request.Lines,
                buildUpCommercialBasis, buildUpProgrammeImpact, buildUpExclusions);
            buildUpModalOpen = false;
            await ReloadAsync();
        }
        // The endpoint answers a refused staging with 400 and its own words (no toast, by
        // convention) — so they land in the dialog the user is still standing in.
        catch (CommandFailedException ex) { buildUpError = ex.Message; }
        catch { buildUpError = "Couldn't stage the build-up. Please try again."; }
        finally { busy = false; }
    }
}
