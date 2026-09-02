using Jewel.JPMS.Api.Features.AccessRequests.Commands;
using Jewel.JPMS.Contracts.AccessRequests;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> AccessRequestActions() => new AiAction[]
    {
        new AiAction(
            Name: "submit_access_request",
            Area: "Access requests",
            Description: "Submits (or refreshes) a pending portal access request for the signed-in "
                + "user's own email — it appears on the administrators' pending access requests "
                + "list. Calling again for the same email updates the display name and request "
                + "time rather than creating a duplicate.",
            CommandType: typeof(SubmitAccessRequest),
            ResultType: typeof(AccessRequest),
            AuthorisationType: typeof(SubmitAccessRequestAuthorisation),
            ValidationType: typeof(SubmitAccessRequestValidation),
            VisibleTo: AnySignedInRole,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "email must be the signed-in user's own email — the authorisation rejects any "
                + "other value. Further per-record checks apply at execution."),

        new AiAction(
            Name: "resolve_access_request",
            Area: "Access requests",
            Description: "Resolves a pending access request by DELETING its row permanently — the "
                + "request disappears from the pending list and there is no undo. This does not "
                + "itself grant or deny access; it only clears the request.",
            CommandType: typeof(ResolveAccessRequest),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(ResolveAccessRequestAuthorisation),
            ValidationType: typeof(ResolveAccessRequestValidation),
            VisibleTo: AdminGateRoles,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "email is the requester's email as listed by the pending access requests view. "
                + "Irreversible — confirm with the user before calling."),
    };
}
