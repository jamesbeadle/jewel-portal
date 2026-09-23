namespace Jewel.JPMS.Models;

/// <summary>
/// Who works the forms. The office sends forms and packs and reads what comes back; the two
/// restricted stores are narrower, because right-to-work evidence and a payroll starter's answers
/// (NI number, date of birth) are seen only by the people who need them. Emergency health answers
/// are revealed on demand — to the office and to whoever is on site when something happens — and
/// every reveal is written to the audit trail.
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

    public static RoleSet ReadersOf(FormEvidenceStore store) => store switch
    {
        FormEvidenceStore.RightToWork => RightToWorkReaders,
        FormEvidenceStore.PayrollStarters => PayrollStarterReaders,
        _ => Office
    };
}
