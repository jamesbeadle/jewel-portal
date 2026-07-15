using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class UpdateProgressReportAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, UpdateProgressReport command) => Allows(user);
}
