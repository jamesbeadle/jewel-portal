using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Drawings;

namespace Jewel.JPMS.Api.Features.Drawings.Commands;

public sealed class RegisterDrawingAuthorisation
{
    private static readonly RoleSet RolesThatMayRegisterDrawings =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RegisterDrawing command) =>
        RolesThatMayRegisterDrawings.IncludesAny(user.Roles);
}
