namespace Jewel.JPMS.Models;

/// <summary>
/// Who does what to the manual. The office maintains it (the directors, the project managers, the
/// compliance coordinator, office admin and the H&amp;S lead); a smaller set approves, because an
/// approval is what puts words in front of the site; every member of staff, the site floor included,
/// reads the view for their role and acknowledges it.
/// </summary>
public static class ManualRoles
{
    public static readonly RoleSet AllowedToManage = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.HealthAndSafetyLead);

    public static readonly RoleSet AllowedToApprove = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.HealthAndSafetyLead);

    public static readonly RoleSet AllowedToReadMaster = JpmsRoleSets.AllInternal;

    public static readonly RoleSet AllowedToReadViews = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator,
        JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead, JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing, JpmsRoles.Foreman, JpmsRoles.Accounts,
        JpmsRoles.SiteOperative);
}

/// <summary>The manual's columns, stated once for the schema, the service and the form.</summary>
public static class ManualLimits
{
    public const int Code = StoredTextLengths.Code;
    public const int Title = StoredTextLengths.Title;
    public const int Purpose = 1000;
    public const int ChangeSummary = 2000;
    public const int LinkedStandards = 1000;
    public const int SourceSections = 256;
    public const int LinkedFormSlugs = 1000;
    public const int TypedName = StoredTextLengths.Name;
    public const int Email = StoredTextLengths.Name;
}
