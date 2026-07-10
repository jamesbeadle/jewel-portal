using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

// Undo a triage decision: send every email currently linked to this request back to the triage
// queue so the thread can be processed again from scratch. The request and its conversation
// history are kept — triage only assigns email context to records, so undoing it must never
// destroy the records themselves. The one exception: a stranded request with no live project is
// removed after its emails return, which is how the unassigned-requests recovery flow disposes
// of broken rows. Restricted to triagers.
public sealed record ReturnRequestToTriage(string RequestId) : ICommand<Acknowledgement>;
