using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class PrepareSubcontractorStatementEmailDraftAuthorisation
{
    // Statement emails are commercial correspondence with the supply chain: the same circle that
    // may email work orders, plus the finance director who owns the account reconciliations.
    private static readonly RoleSet RolesThatMayEmailStatements = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager,
        JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, PrepareSubcontractorStatementEmailDraft command) =>
        RolesThatMayEmailStatements.IncludesAny(user.Roles);
}
