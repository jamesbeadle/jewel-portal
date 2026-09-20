using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class SendSubcontractorStatementEmailAuthorisation
{
    // Statement emails are commercial correspondence with the supply chain, and writing to the
    // supply chain as the business is the directors' and the project manager's (Nigel, 2026-09-19).
    // The office circle — compliance coordinator, office admin, sales — ran these until then and
    // now prepares them for a director to send. Borrowed by SubcontractorStatementEmailComposer so
    // the preview reaches who the send does.
    internal static readonly RoleSet RolesThatMayEmailStatements = RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SendSubcontractorStatementEmail command) =>
        RolesThatMayEmailStatements.IncludesAny(user.Roles);
}
