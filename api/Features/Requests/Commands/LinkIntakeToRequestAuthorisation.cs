using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class LinkIntakeToRequestAuthorisation
{
    public bool Allows(SignedInUser user, LinkIntakeToRequest command) => TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
