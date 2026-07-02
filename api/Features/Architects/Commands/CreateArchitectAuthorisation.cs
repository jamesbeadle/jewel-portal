using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

public sealed class CreateArchitectAuthorisation
{
    public bool Allows(SignedInUser user, CreateArchitect command) =>
        ArchitectRoles.AllowedToManageArchitects.IncludesAny(user.Roles);
}
