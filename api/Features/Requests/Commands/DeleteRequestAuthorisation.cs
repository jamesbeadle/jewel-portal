using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Deleting a request is destructive and removes the whole conversation history,
// so it is restricted to master administrators only.
public sealed class DeleteRequestAuthorisation
{
    public bool Allows(SignedInUser user, DeleteRequest command) =>
        JpmsAdministrators.Contains(user.Email);
}
