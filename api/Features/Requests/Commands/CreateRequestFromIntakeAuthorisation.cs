using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class CreateRequestFromIntakeAuthorisation
{
    public bool Allows(SignedInUser user, CreateRequestFromIntake command) => TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
