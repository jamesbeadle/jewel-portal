using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class DeleteDirectoryUserAuthorisation
{
    public bool Allows(SignedInUser user, DeleteDirectoryUser command) =>
        AdminGate.Allows(user);
}
