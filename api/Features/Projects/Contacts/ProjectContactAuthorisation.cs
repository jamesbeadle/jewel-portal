using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

// Managing the people a project's requests are issued to is a project-administration action,
// so it is limited to the same management roles that may create and run projects.
public sealed class ProjectContactAuthorisation
{
    private static readonly RoleSet RolesThatMayManageContacts =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user) => RolesThatMayManageContacts.IncludesAny(user.Roles);
}
