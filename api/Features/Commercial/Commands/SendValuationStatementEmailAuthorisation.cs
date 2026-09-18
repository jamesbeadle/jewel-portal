using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SendValuationStatementEmailAuthorisation
{
    // Valuation statements are client-facing money correspondence: the circle that runs the
    // valuation report and its claims, not the wider internal-read set that may merely view them.
    // Borrowed by ValuationStatementEmailComposer so the preview reaches exactly who the send does.
    internal static readonly RoleSet RolesThatMayEmailStatements = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SendValuationStatementEmail command) =>
        RolesThatMayEmailStatements.IncludesAny(user.Roles);
}
