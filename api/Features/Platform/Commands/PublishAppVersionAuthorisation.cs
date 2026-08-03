using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform.Commands;

public sealed class PublishAppVersionAuthorisation
{
    public bool Allows(SignedInUser user, PublishAppVersion command) =>
        AdminGate.Allows(user);
}
