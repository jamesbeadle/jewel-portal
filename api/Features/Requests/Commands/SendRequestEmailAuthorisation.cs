using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Drafting the outbound email is a step short of sending it, but it stages an external communication
// in the shared mailbox — so it carries the same gate as resending a request document.
public sealed class SendRequestEmailAuthorisation
{
    // Borrowed by RequestEmailComposer so the preview reaches exactly who the send does.
    internal static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, SendRequestEmail command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
