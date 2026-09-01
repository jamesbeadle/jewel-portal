using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Deleting a staged draft is the undo of staging one, so it carries exactly the drafting gate
// (PrepareRequestEmailDraft / PrepareRequestReplyDraft): whoever may put a draft in the shared
// mailbox may also withdraw one. The mailbox client itself refuses anything that is not an unsent
// draft, so this gate never reaches sent or received mail whatever the caller's roles.
public sealed class DeleteMailboxDraftAuthorisation
{
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, DeleteMailboxDraft command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
