namespace Jewel.JPMS.Models;

/// <summary>
/// Who instructs site. Internal: the people who run the job — the same set as defect and inventory
/// maintenance — and no external role. The page and the endpoint both read this, so a button is
/// never shown to a role the API refuses.
/// </summary>
public static class SiteInstructionRoles
{
    public static readonly RoleSet Managers = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager);
}
