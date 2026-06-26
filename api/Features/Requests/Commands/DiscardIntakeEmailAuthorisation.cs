using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class DiscardIntakeEmailAuthorisation
{
    public bool Allows(SignedInUser user, DiscardIntakeEmail command) => TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
