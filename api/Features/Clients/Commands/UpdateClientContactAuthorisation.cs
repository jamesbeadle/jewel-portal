using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Clients;

namespace Jewel.JPMS.Api.Features.Clients.Commands;

public sealed class UpdateClientContactAuthorisation
{
    public bool Allows(SignedInUser user, UpdateClientContact command) =>
        ClientRoles.AllowedToManageClients.IncludesAny(user.Roles);
}
