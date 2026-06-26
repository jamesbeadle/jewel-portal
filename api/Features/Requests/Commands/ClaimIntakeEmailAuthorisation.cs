using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ClaimIntakeEmailAuthorisation
{
    public bool Allows(SignedInUser user, ClaimIntakeEmail command) => TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
