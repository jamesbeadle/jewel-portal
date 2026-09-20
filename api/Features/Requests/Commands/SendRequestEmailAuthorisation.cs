using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Drafting the outbound email is a step short of sending it, but it stages an external communication
// in the shared mailbox — so it carries the same gate as resending a request document.
//
// Narrowed on 2026-09-19 (Nigel, from the permission check): the Architect — an EXTERNAL role —
// could stage and withdraw mail in Jewel's own projects mailbox, and the Site Manager could issue
// it. Staging mail that leaves the business as the business is the directors' and the PM's.
public sealed class SendRequestEmailAuthorisation
{
    // Borrowed by RequestEmailComposer so the preview reaches exactly who the send does.
    internal static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SendRequestEmail command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
