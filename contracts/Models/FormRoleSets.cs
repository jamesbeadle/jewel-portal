namespace Jewel.JPMS.Models;

/// <summary>
/// Who works the forms. The office sends forms and packs and reads what comes back; the two
/// restricted stores are narrower, because right-to-work evidence and a payroll starter's answers
/// (NI number, date of birth) are seen only by the people who need them. Emergency health answers
/// are revealed on demand — to the office and to whoever is on site when something happens — and
/// every reveal is written to the audit trail. The health and safety store — the officer's site
/// checks and the site's incident reports — is read by the office and by the site manager and the
/// H&amp;S officer, whose forms they are.
/// </summary>
public static class FormRoleSets
{
    public static readonly RoleSet Office = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin,
        JpmsRoles.Accounts);

    public static readonly RoleSet RightToWorkReaders = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.OfficeComplianceCoordinator);

    public static readonly RoleSet PayrollStarterReaders = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.Accounts);

    public static readonly RoleSet EmergencyContactReaders = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin);

    public static readonly RoleSet HealthAndSafetyReaders = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin,
        JpmsRoles.Accounts);

    /// <summary>Everyone who may open the Received list: the office, and the site roles for the forms in the health and safety store.</summary>
    public static readonly RoleSet AnyReader = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager,
        JpmsRoles.HealthAndSafetyLead,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin,
        JpmsRoles.Accounts);

    public static RoleSet ReadersOf(FormEvidenceStore store) => store switch
    {
        FormEvidenceStore.RightToWork => RightToWorkReaders,
        FormEvidenceStore.PayrollStarters => PayrollStarterReaders,
        FormEvidenceStore.HealthAndSafety => HealthAndSafetyReaders,
        _ => Office
    };
}
