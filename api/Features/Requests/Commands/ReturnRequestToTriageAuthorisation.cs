using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Returning a request to triage is a triage action (undoing a triage decision), so it is open to
// the same staff who triage: administrators and project managers.
public sealed class ReturnRequestToTriageAuthorisation
{
    public bool Allows(SignedInUser user, ReturnRequestToTriage command) => TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}
