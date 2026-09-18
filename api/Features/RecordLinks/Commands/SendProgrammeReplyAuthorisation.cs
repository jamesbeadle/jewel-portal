using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

public sealed class SendProgrammeReplyAuthorisation
{
    // A reply sends an external communication from the shared mailbox — the same act as the
    // request reply, so the same shape of gate: the roles that speak for the project (directors,
    // project managers, site managers; admins carry every role server-side). The architect —
    // present on the request gate for RFIs — has no programme-communications surface, so is
    // deliberately absent here.
    private static readonly RoleSet RolesThatMayReply =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user, SendProgrammeReply command) =>
        RolesThatMayReply.IncludesAny(user.Roles);
}
