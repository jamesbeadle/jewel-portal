namespace Jewel.JPMS.Models;

/// <summary>
/// Who reads the Xero reports. The aged payables and receivables are the allocation queue's
/// own figures aggregated, so the same finance-facing audience reads them, plus Accounts, whose
/// Weekly Cashflow is seeded from exactly those reads (2026-08-27). The raw transaction ledger
/// is narrower: the cost-codes audience, without the project manager.
/// </summary>
public static class XeroReportRoles
{
    public static readonly RoleSet AgedReportReaders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.Accounts);

    public static readonly RoleSet TransactionReaders = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);
}
