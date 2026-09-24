namespace Jewel.JPMS.Models;

/// <summary>
/// The Monday replacement (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §8): company
/// registers (insurances, subscriptions, vans, trade accounts) and staff sign-off forms.
/// Register admin and policy publishing sit with the office/director roles; signing is every
/// user's own surface, resolved by their signed-in email — no impersonation.
/// </summary>
public static class RegisterRoleSets
{
    public static readonly RoleSet ManageRegisters = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.OfficeComplianceCoordinator);

    /// <summary>Who reads the published policies and who has signed them: the register's managers, and
    /// the forms office, who send the Policy sign-off form and chase it.</summary>
    public static readonly RoleSet PolicyReaders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.Accounts);
}
