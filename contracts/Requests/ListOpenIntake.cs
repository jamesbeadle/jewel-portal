using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The triage queue: every email that has been ingested from requests@ and not yet resolved
// (still NeedsTriage or currently Claimed). Linked and Discarded emails drop out of the queue.
public sealed record ListOpenIntake() : IQuery<IReadOnlyList<IntakeEmail>>;
