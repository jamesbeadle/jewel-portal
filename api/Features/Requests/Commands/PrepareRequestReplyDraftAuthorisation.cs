using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// A reply draft stages an external communication in the shared mailbox, exactly like drafting the
// outbound document email — so it carries the same gate.
public sealed class PrepareRequestReplyDraftAuthorisation
{
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, PrepareRequestReplyDraft command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
