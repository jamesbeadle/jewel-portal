using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RestoreDirectoryUserAuthorisation
{
    public bool Allows(SignedInUser user, RestoreDirectoryUser command) =>
        AdminGate.Allows(user);
}
