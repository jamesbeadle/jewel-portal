using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    // ---- Header and facts edits: the dialogs own their drafts; the Actions menu opens them ----

    private void OpenHeaderEdit() { if (record is not null) editingHeader = true; }

    private void OpenFactsEdit() { if (record is not null) editingFacts = true; }
}
