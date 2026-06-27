using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Undo a triage decision: send every email currently linked to this request back to the triage
// queue so the thread can be processed again from scratch. If returning those emails leaves the
// request with no linked emails, the request itself is deleted (the triage was a mistake and there
// is nothing left to keep). Restricted to triagers.
public sealed record ReturnRequestToTriage(string RequestId) : ICommand<Acknowledgement>;
