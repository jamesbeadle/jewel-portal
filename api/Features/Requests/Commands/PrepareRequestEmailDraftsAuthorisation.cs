using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Bulk drafting stages the same external communications as drafting one at a time, so it carries
// the same gate as PrepareRequestEmailDraftAuthorisation — the count doesn't change the act.
public sealed class PrepareRequestEmailDraftsAuthorisation
{
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, PrepareRequestEmailDrafts command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
