using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

public sealed class SendProgrammeReplyAuthorisation
{
    // A reply sends an external communication from the shared mailbox — the same act as the
    // request reply, so the same gate: the directors and the project manager (Nigel, 2026-09-19 —
    // writing as the business is theirs). The site manager was here until then and issued the
    // programme's mail; they raise it and a director sends it now. The architect — present on the
    // request gate for RFIs until the same decision — has no programme-communications surface
    // either way.
    private static readonly RoleSet RolesThatMayReply =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SendProgrammeReply command) =>
        RolesThatMayReply.IncludesAny(user.Roles);
}
