using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Bulk drafting stages the same external communications as drafting one at a time, so it carries
// the same gate as SendRequestEmailAuthorisation — the count doesn't change the act.
public sealed class SendRequestEmailsAuthorisation
{
    private static readonly RoleSet RolesThatMayDraft =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SendRequestEmails command) => RolesThatMayDraft.IncludesAny(user.Roles);
}
