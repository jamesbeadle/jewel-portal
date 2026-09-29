namespace Jewel.JPMS.Models;

/// <summary>
/// Who may attach to a request. Every reader of the request may see its attachments; the delivery
/// team and the architect answering it may add and remove them (the architect on their own
/// projects, scoped at the endpoint). The page and the endpoint both read this.
/// </summary>
public static class RequestAttachmentRoles
{
    public static readonly RoleSet Readers = JpmsRoleSets.DeliveryTeamAndParties;

    public static readonly RoleSet AllowedToAttach = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.SiteManager,
        JpmsRoles.Foreman,
        JpmsRoles.Architect);
}
