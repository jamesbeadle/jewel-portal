using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The email isn't a request (spam, an out-of-office, a misdirected message). It leaves the
// queue as Discarded but is kept on record so it is never silently lost or re-ingested.
public sealed record DiscardIntakeEmail(
    string IntakeId,
    string? Notes = null) : ICommand<IntakeEmail>;
