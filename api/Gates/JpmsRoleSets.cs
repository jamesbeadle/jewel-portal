namespace Jewel.JPMS.Api.Gates;

/// <summary>
/// Shared role sets for endpoint authorisation. The floor for every endpoint is a role check —
/// "is signed in" alone is never enough, because external logins (subcontractor portal, and in
/// future clients/architects) carry valid session cookies too. Administrators pass every gate
/// (SignedInUserResolver grants them all roles).
///
/// Guidance (see docs/05-data-model/permissions-matrix.md): external roles are never owners of
/// internal workflows — default internal queries to AllInternal and add external roles only where
/// the matrix names them (e.g. Architect on RFI/variation approval reads, Subcontractor on
/// drawings for their assigned work).
/// </summary>
public static class JpmsRoleSets
{
    /// <summary>Every internal (JBB staff) role. Mirrors DesktopNavigation.AllInternalRoles.</summary>
    public static readonly RoleSet AllInternal = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.Foreman);

    /// <summary>Internal roles plus the architect, who reads/approves RFIs, submittals and
    /// variations per the permissions matrix.</summary>
    public static readonly RoleSet InternalAndArchitect = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.Foreman,
        JpmsRoles.Architect);

    /// <summary>Drawing readers: internal roles plus the externals who work from drawings —
    /// architects issue them, subcontractors read revisions for their assigned work (P10).</summary>
    public static readonly RoleSet DrawingReaders = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.Foreman,
        JpmsRoles.Architect,
        JpmsRoles.Subcontractor);

    /// <summary>The commercial team: money-facing reads (cashflow, Xero, ledger detail) that the
    /// wider site team has no business seeing. Mirrors the Xero/Allocation navigation gates.</summary>
    public static readonly RoleSet CommercialTeam = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator);
}
