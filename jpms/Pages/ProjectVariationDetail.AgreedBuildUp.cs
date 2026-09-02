using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // The panel staged the build-up and handed back the record; the estimate and the
    // approve modal's seed both read from it, so re-read the page around it.
    private async Task OnBuildUpStaged(VariationOrder staged)
    {
        order = staged;
        await ReloadAsync();
    }

    private async Task OnTenderRecorded(VariationOrder recorded)
    {
        order = recorded;
        await ReloadAsync();
    }
}
