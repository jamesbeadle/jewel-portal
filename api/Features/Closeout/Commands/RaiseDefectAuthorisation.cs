using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class RaiseDefectAuthorisation
{
    public bool Allows(SignedInUser user, RaiseDefect command) => DefectRoles.Raisers.IncludesAny(user.Roles);
}
