using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

public sealed class CreateProgressUpdateAuthorisation
{
    // Site Managers collate progress on site; Project Managers and the Managing Director may
    // contribute too (the MD helps out). Administrators pass every gate.
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, CreateProgressUpdate command) => Allows(user);
}
