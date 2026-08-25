using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

// Labelling a revision is register curation — the same gate as editing a drawing's code and title.
public sealed class SetDrawingRevisionLabelAuthorisation
{
    private static readonly RoleSet RolesThatMayEditDrawings =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SetDrawingRevisionLabel command) =>
        RolesThatMayEditDrawings.IncludesAny(user.Roles);
}
