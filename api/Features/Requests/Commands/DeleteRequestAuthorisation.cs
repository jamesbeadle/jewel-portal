using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Deleting a request is destructive and removes the whole conversation history, so it is
// restricted to the Admin role only. Master administrators hold every role server-side
// (UserRoles.ForAsync), so they qualify through Role.Admin like any directory admin.
public sealed class DeleteRequestAuthorisation
{
    private static readonly RoleSet AllowedToDelete = RoleSet.Of(Role.Admin);

    public bool Allows(SignedInUser user, DeleteRequest command) =>
        AllowedToDelete.IncludesAny(user.Roles);
}
