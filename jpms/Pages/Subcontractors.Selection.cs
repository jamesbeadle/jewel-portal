using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Features.Directory;
using static Jewel.JPMS.Features.Directory.DirectoryDisplay;

namespace Jewel.JPMS.Pages;

public partial class Subcontractors
{
    // Tick 2+ records, then Consolidate opens over them — the dialog owns the merge.
    private readonly HashSet<string> selectedIds = new(StringComparer.OrdinalIgnoreCase);
    private XeroImportModal importModal = default!;
    private ConsolidateRecordsModal consolidateModal = default!;

    private void SetSelected(string subcontractorId, bool ticked)
    {
        if (ticked) selectedIds.Add(subcontractorId);
        else selectedIds.Remove(subcontractorId);
    }

    // Selection intersected with the current directory, so a stale tick (record merged away in
    // another tab) can never enter a consolidation.
    private List<Subcontractor> SelectedForConsolidation() =>
        SubcontractorStore.All().Where(sub => selectedIds.Contains(sub.SubcontractorId)).ToList();
}
