using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class UpdateDrawingMetadataAuthorisation
{
    private static readonly RoleSet RolesThatMayEditDrawings =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, UpdateDrawingMetadata command) =>
        RolesThatMayEditDrawings.IncludesAny(user.Roles);
}
