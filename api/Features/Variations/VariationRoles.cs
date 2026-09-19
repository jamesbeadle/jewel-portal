
namespace Jewel.JPMS.Api.Features.Variations;

// Raising and managing variation order quotes is a commercial/PM task: Administrator, Managing
// Director, Finance Director, Project Manager and QS (Estimator). Administrators carry every role
// server-side.
//
// The Finance Director was added on 2026-09-19, from Nigel: a variation is a commercial record and
// raising one is the FD's work as much as the PM's. Until then the FD could start a valuation claim
// but not a variation order at all — not create one, price it, revise it, issue it or email it,
// only post a message on one — which the permission check found by reading the acceptance criteria
// on the task ("the FD can start a variation order and a valuation claim") against the gate.
//
// Approving is NOT widened with it: that stays AllowedToApproveVariations below, because approval
// is the client's instruction to spend rather than a commercial edit.
internal static class VariationRoles
{
    public static readonly RoleSet AllowedToManageVariations =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Approving a VOQ (raising the VO onto the contract figures) additionally belongs to the
    // client — variations are ultimately the client's instruction to spend. Internal roles stay
    // PM-and-above (plus QS, who prepares the commercial records the approval writes to).
    public static readonly RoleSet AllowedToApproveVariations =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.Client);
}
