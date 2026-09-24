namespace Jewel.JPMS.Models;

/// <summary>Who opens the Owner Overview — the board. The same set as the API's gate on the
/// Xero bank position (GetXeroCashSummary's AllowedToViewCash), because the page's first
/// figure is that position and every other figure on it the board already reads elsewhere.</summary>
public static class OwnerOverviewRoles
{
    public static readonly RoleSet Owners = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);
}
