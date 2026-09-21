namespace Jewel.JPMS.Models;

/// <summary>
/// Who does what with a site audit. The whole internal team reads it — where the site stands on
/// safety concerns everyone on the job. Running one (planting it, filling it in, issuing and
/// closing it) is the H&S officer's work, with the directors, the PM and the site manager who
/// answer for the site beside her, and the compliance coordinator who files the paperwork. The
/// same people as LogHsRecordAuthorisation, plus Admin and the compliance coordinator.
/// </summary>
public static class HsAuditRoles
{
    public static readonly RoleSet Readers = JpmsRoleSets.AllInternal;

    public static readonly RoleSet Auditors = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator);
}
