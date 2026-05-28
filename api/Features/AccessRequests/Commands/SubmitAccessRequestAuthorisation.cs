using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class SubmitAccessRequestAuthorisation
{
    public bool Allows(SignedInUser user, SubmitAccessRequest command) =>
        string.Equals(user.Email, command.Email, StringComparison.OrdinalIgnoreCase);
}
