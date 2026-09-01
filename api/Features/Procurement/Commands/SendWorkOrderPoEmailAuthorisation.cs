using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SendWorkOrderPoEmailAuthorisation
{
    // The send fires automatically the moment an order is released, so everyone who can release
    // one — create it un-drafted or approve a draft (CreateManualWorkOrder / ApproveWorkOrder
    // roles) — must be able to send it, plus the roles that may email work orders manually from
    // the PO page (PrepareWorkOrderEmailDraftAuthorisation).
    private static readonly RoleSet RolesThatMaySendPoEmails =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
            JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, SendWorkOrderPoEmail command) => RolesThatMaySendPoEmails.IncludesAny(user.Roles);
}
