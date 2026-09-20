using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// A reply draft stages an external communication in the shared mailbox, exactly like drafting the
// fresh work-order email — so it carries the same gate as SendWorkOrderPoEmailAuthorisation.
public sealed class PrepareWorkOrderReplyDraftAuthorisation
{
    // Replying to a supplier from the shared mailbox is writing as the business, which is the
    // directors' and the project manager's (Nigel, 2026-09-19). The office circle raised and
    // answered these until then; they still run the order, a director sends the mail.
    private static readonly RoleSet RolesThatMayEmailWorkOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, PrepareWorkOrderReplyDraft command) => RolesThatMayEmailWorkOrders.IncludesAny(user.Roles);
}
