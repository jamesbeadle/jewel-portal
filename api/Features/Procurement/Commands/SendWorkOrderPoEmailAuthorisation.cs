using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendWorkOrderPoEmailAuthorisation
{
    // The send fires automatically the moment an order is released, so everyone who can release
    // one — create it un-drafted or approve a draft (CreateManualWorkOrder / ApproveWorkOrder
    // roles) — must be able to send it, plus the roles that may email work orders by hand from the
    // PO page or the tender award. This is the gate for the draft-only path too since the two
    // commands were folded into one (2026-09-17); it is the wider of the two sets, so nobody who
    // could stage a draft before has lost the door.
    private static readonly RoleSet RolesThatMaySendPoEmails =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
            JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SendWorkOrderPoEmail command) => RolesThatMaySendPoEmails.IncludesAny(user.Roles);
}
