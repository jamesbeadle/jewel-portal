using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The triage queue: every email that has been ingested from requests@ and not yet resolved
// (still NeedsTriage or currently Claimed). Linked, Discarded and RemovedFromMailbox emails
// drop out of the queue. Paged server-side so the queue mirrors the Inbox without loading it
// all into the client.
public sealed record ListOpenIntake(int Skip = 0, int Take = 25) : IQuery<PagedResult<IntakeEmail>>;
