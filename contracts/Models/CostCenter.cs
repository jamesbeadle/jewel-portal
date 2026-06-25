namespace Jewel.JPMS.Models;

// Global cost-center master. One shared hierarchy referenced by every project's
// Financials tab — managed centrally rather than per project.
public sealed record CostCenter(
    string CostCenterId,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive = true);
