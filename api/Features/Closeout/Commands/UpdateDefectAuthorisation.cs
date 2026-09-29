using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class UpdateDefectAuthorisation
{
    public bool Allows(SignedInUser user, UpdateDefect command) => DefectRoles.Managers.IncludesAny(user.Roles);
}
