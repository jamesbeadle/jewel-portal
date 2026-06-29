using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RestoreIntakeEmailAuthorisation
{
    public bool Allows(SignedInUser user, RestoreIntakeEmail command) => TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
