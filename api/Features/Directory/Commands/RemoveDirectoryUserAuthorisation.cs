using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RemoveDirectoryUserAuthorisation
{
    public bool Allows(SignedInUser user, RemoveDirectoryUser command) =>
        JpmsAdministrators.Contains(user.Email);
}
