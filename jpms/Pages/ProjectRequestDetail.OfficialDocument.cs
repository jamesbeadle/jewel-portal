using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    // ---- Official document form ----------------------------------------------------------------
    // The editor itself (rows, sections, save) is RequestOfficialFormPanel's; opening it from the
    // Actions menu must land the user in front of it rather than editing a panel on the tab they
    // are not looking at.
    private void OpenFormEditor()
    {
        if (record is null) return;
        if (HasOfficialTab) activeTab = "official";
        editingForm = true;
    }
}
