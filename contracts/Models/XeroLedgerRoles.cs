namespace Jewel.JPMS.Models;

/// <summary>
/// Who may work the allocation queue: financially sensitive, so the same
/// finance-facing audience as the Xero ledger view and the cost-code master.
/// Admins pass because Role.Admin is included explicitly.
/// </summary>
public static class XeroLedgerRoles
{
    public static readonly RoleSet AllowedToAllocate = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
}
