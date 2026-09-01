using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class UpdateManualWorkOrderAuthorisation
{
    // The same roles that may raise a manual order may correct one — editing is part of
    // the same reconciliation duty (see CreateManualWorkOrderAuthorisation).
    private static readonly RoleSet RolesThatMayEditOrders =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // The wider power — editing orders a source flow owns (tender award, variation
    // instruction, Buildertrend seed) — is a directors' money decision, same set as
    // CancelWorkOrderAuthorisation. The endpoint stamps this onto the command
    // (EditorMayEditAnyOrder); the handler enforces it against the actual order.
    private static readonly RoleSet RolesThatMayEditAnyOrder =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public bool Allows(SignedInUser user, UpdateManualWorkOrder command) =>
        RolesThatMayEditOrders.IncludesAny(user.Roles);

    public static bool MayEditAnyOrder(SignedInUser user) =>
        RolesThatMayEditAnyOrder.IncludesAny(user.Roles);
}
