using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class ResolveAccessRequestAuthorisation
{
    public bool Allows(SignedInUser user, ResolveAccessRequest command) =>
        user.Roles.Contains(Role.Admin);
}
