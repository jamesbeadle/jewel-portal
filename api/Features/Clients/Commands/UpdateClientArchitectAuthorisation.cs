using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class UpdateClientArchitectAuthorisation
{
    public bool Allows(SignedInUser user, UpdateClientArchitect command) =>
        ClientRoles.AllowedToManageClients.IncludesAny(user.Roles);
}
