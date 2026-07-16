using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Reconciliation packages tie the two sides of one procurement together at the level
/// the work was actually bought and sold: work orders on the cost side, valuation
/// sales lines (whole or a partial £ share) on the sales side. Presentation only —
/// nothing writes to Xero and dissolving a package changes nothing underneath. Every
/// package's definitions, for the builder and the unallocated view.
/// </summary>
public sealed record ListReconciliationPackagesForProject(string ProjectId) : IQuery<IReadOnlyList<ReconciliationPackage>>;

/// <summary>The computed report rows: one per package, figures live from source (or the
/// frozen snapshot for locked packages).</summary>
public sealed record ListPackageReconciliation(string ProjectId) : IQuery<IReadOnlyList<PackageReconciliationRow>>;

/// <summary>
/// Creates or replaces a package's whole definition (null id = create). Guards: a work
/// order sits in at most one package; a sales line's value may be shared across
/// packages by partial slices but never over-allocated; a direct cost slice may never
/// take a purchase line past its non-work-order remainder (and only whole-line
/// allocations can be sliced); locked packages can't be edited.
/// </summary>
public sealed record SaveReconciliationPackage(
    string ProjectId,
    string? ReconciliationPackageId,
    string Name,
    IReadOnlyList<string> WorkOrderIds,
    IReadOnlyList<PackageSalesSlice> SalesLines,
    // Direct purchase costs: £ slices of allocated Xero lines not paying any work
    // order — materials bought directly for the packaged scope. Null = none (kept
    // optional so older callers are unaffected).
    IReadOnlyList<PackageCostSlice>? CostLines = null) : ICommand<ReconciliationPackage>;

/// <summary>Dissolves a package (must be unlocked). Nothing else is deleted.</summary>
public sealed record RemoveReconciliationPackage(
    string ProjectId,
    string ReconciliationPackageId) : ICommand<Acknowledgement>;

/// <summary>
/// Locks a package — freezing its figures and realising profit / loss against ACTUAL
/// invoiced cost (target cost − invoiced to date), not committed orders, so buying
/// gains are only banked on what was really paid. Unlocking clears the snapshot and
/// the figures go live again.
/// </summary>
public sealed record SetReconciliationPackageLock(
    string ProjectId,
    string ReconciliationPackageId,
    bool IsLocked) : ICommand<ReconciliationPackage>;
