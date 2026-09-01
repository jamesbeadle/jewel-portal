using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RemoveDirectoryUserAuthorisation
{
    public bool Allows(SignedInUser user, RemoveDirectoryUser command) =>
        AdminGate.Allows(user);
}
