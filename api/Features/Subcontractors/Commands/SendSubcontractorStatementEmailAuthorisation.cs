using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class SendSubcontractorStatementEmailAuthorisation
{
    // Statement emails are commercial correspondence with the supply chain: the same circle that
    // may email work orders, plus the finance director who owns the account reconciliations.
    // Borrowed by SubcontractorStatementEmailComposer so the preview reaches who the send does.
    internal static readonly RoleSet RolesThatMayEmailStatements = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    public bool Allows(SignedInUser user, SendSubcontractorStatementEmail command) =>
        RolesThatMayEmailStatements.IncludesAny(user.Roles);
}
