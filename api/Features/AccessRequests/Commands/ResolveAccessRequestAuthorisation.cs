using Jewel.JPMS.Contracts.AccessRequests;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class ResolveAccessRequestAuthorisation
{
    public bool Allows(SignedInUser user, ResolveAccessRequest command) =>
        AdminGate.Allows(user);
}
