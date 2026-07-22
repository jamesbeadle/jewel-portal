using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class UpsertDirectoryUserAuthorisation
{
    public bool Allows(SignedInUser user, UpsertDirectoryUser command) =>
        AdminGate.Allows(user);
}
