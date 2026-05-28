using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class IssueDrawingRevisionAuthorisation
{
    private static readonly RoleSet RolesThatMayIssueRevisions =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, IssueDrawingRevision command) =>
        RolesThatMayIssueRevisions.IncludesAny(user.Roles);
}
