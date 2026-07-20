using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

/// <summary>
/// Pages that share one side-nav entry as sibling tabs: the Xero entry (Transactions +
/// Allocation) and the Financials Setup entry (Cost codes + Rates). WorkspaceSectionNav renders
/// the owning section's tab row on each page. Per-tab role lists reproduce the visibility the
/// pages had as standalone side-nav entries — grouping widens nothing. Administrators see every
/// tab (mirrored from DesktopNavigation's admin early-return).
/// </summary>
public sealed record WorkspaceTab(string Label, string Href, IReadOnlyList<Role> VisibleTo)
{
    public bool IsVisibleTo(Role role) => role == Role.Admin || VisibleTo.Contains(role);
}

public sealed record WorkspaceSectionInfo(string Label, IReadOnlyList<WorkspaceTab> Tabs)
{
    public bool Owns(string path) => BestMatch(path) is not null;

    /// <summary>
    /// The tab that owns a path — exact match or a sub-route of it. Longest href wins so nested
    /// routes activate the most specific tab.
    /// </summary>
    public WorkspaceTab? BestMatch(string path) =>
        Tabs.Where(tab => path == tab.Href || path.StartsWith(tab.Href + "/", StringComparison.Ordinal))
            .OrderByDescending(tab => tab.Href.Length)
            .FirstOrDefault();

    public IReadOnlyList<WorkspaceTab> TabsVisibleTo(Role role) =>
        Tabs.Where(tab => tab.IsVisibleTo(role)).ToList();
}

public static class WorkspaceSections
{
    public static readonly WorkspaceSectionInfo Xero = new(
        "Xero",
        new[]
        {
            // Allocation leads — it's the working screen; Transactions is the reference view.
            // Mirrors the API's allocation authorisation (XeroLedgerRoles.AllowedToAllocate).
            new WorkspaceTab("Allocation", "/finance/allocation",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.ProjectManager, Role.QuantitySurveyor }),
            // Mirrors the API's Xero ledger authorisation (ListXeroTransactionsEndpoint).
            new WorkspaceTab("Transactions", "/finance/xero",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.QuantitySurveyor })
        });

    public static readonly WorkspaceSectionInfo FinanceSetup = new(
        "Setup",
        new[]
        {
            // Mirrors the API's cost-centre command authorisation.
            new WorkspaceTab("Cost codes", "/cost-codes",
                new[] { Role.ManagingDirector, Role.FinanceDirector, Role.QuantitySurveyor }),
            new WorkspaceTab("Rates", "/rate-library",
                new[] { Role.ManagingDirector, Role.ProjectManager, Role.QuantitySurveyor })
        });

    public static readonly IReadOnlyList<WorkspaceSectionInfo> All =
        new[] { Xero, FinanceSetup };

    public static WorkspaceSectionInfo? SectionOwning(string path) =>
        All.FirstOrDefault(section => section.Owns(path));
}
