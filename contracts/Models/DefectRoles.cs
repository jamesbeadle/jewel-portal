namespace Jewel.JPMS.Models;

/// <summary>
/// Who acts on a defect. The people who run the job raise and manage them; the project's client and
/// architect may raise one on their own project (snag capture, matrix workflow 08), never manage it.
/// The page and the endpoint both read these, so a button is never shown to a role the API refuses.
/// </summary>
public static class DefectRoles
{
    public static readonly RoleSet Managers = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager);

    public static readonly RoleSet Raisers = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager,
        JpmsRoles.Client,
        JpmsRoles.Architect);
}
