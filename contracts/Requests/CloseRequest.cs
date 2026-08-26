using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Close a request as at a user-chosen date. ClosedAt is the date the request is recorded as
// closed — chosen by the user (defaults to today, may be a prior date when the closure is only
// recorded later; never in the future). Null falls back to the moment the close is processed.
// ClosedByEmail is stamped server-side from the signed-in user; any client-supplied value is
// ignored by the endpoint.
//
// History: this replaced AttemptCloseRequest (2026-08-26) when the per-record agent framework was
// retired. That command's "agent close-gate" always passed once provisioning was switched off
// (2026-07-02), so the gate was ceremony — closing is now unconditional.
public sealed record CloseRequest(string RequestId, string ClosedByEmail = "", DateTimeOffset? ClosedAt = null) : ICommand<RequestCloseOutcome>;

// Result of a close. Closed is false only when the request no longer exists.
public sealed record RequestCloseOutcome(bool Closed);
