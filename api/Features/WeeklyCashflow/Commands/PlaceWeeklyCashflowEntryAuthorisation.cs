using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.WeeklyCashflow;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

public sealed class PlaceWeeklyCashflowEntryAuthorisation
{
    public bool Allows(SignedInUser user, PlaceWeeklyCashflowEntry command) =>
        WeeklyCashflowGates.WeeklyCashflowRoles.IncludesAny(user.Roles);
}
