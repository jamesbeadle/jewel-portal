using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Architects;

namespace Jewel.JPMS.Api.Features.Architects.Commands;

public sealed class UpdateArchitectAuthorisation
{
    public bool Allows(SignedInUser user, UpdateArchitect command) =>
        ArchitectRoles.AllowedToManageArchitects.IncludesAny(user.Roles);
}
