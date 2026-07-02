using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Drafting the outbound email is a step short of sending it, but it stages an external communication
// in the shared mailbox — so it carries the same gate as resending a request document.
public sealed class PrepareRequestEmailDraftAuthorisation
{
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, PrepareRequestEmailDraft command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
