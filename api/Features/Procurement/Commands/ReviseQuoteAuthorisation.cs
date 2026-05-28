using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ReviseQuoteAuthorisation
{
    private static readonly RoleSet RolesThatMayReviseQuotes =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.Subcontractor);

    public bool Allows(SignedInUser user, ReviseQuote command) => RolesThatMayReviseQuotes.IncludesAny(user.Roles);
}
