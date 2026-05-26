using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services.Navigation;

public static class PortalNavigation
{
    public static IReadOnlyList<NavigationItem> ItemsFor(Role role) => role switch
    {
        Role.Architect     => ArchitectItems,
        Role.Client        => ClientItems,
        Role.Subcontractor => SubcontractorItems,
        _ => Array.Empty<NavigationItem>()
    };

    private static readonly IReadOnlyList<NavigationItem> ArchitectItems = new NavigationItem[]
    {
        new("Home",         "/portal/architect"),
        new("RFIs",         "/portal/architect/rfis"),
        new("Submittals",   "/portal/architect/submittals"),
        new("Variations",   "/portal/architect/variations")
    };

    private static readonly IReadOnlyList<NavigationItem> ClientItems = new NavigationItem[]
    {
        new("Your project", "/portal/client")
    };

    private static readonly IReadOnlyList<NavigationItem> SubcontractorItems = new NavigationItem[]
    {
        new("Home",         "/portal/subcontractor"),
        new("Bids",         "/portal/subcontractor/bids"),
        new("Compliance",   "/portal/subcontractor/compliance")
    };
}
