using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class CreateClientAuthorisation
{
    public bool Allows(SignedInUser user, CreateClient command) =>
        ClientRoles.AllowedToManageClients.IncludesAny(user.Roles);
}
