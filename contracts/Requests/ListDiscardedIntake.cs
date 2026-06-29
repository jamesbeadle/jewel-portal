using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// The discarded pile: emails a triager marked as not-a-request (spam, auto-replies, misdirected
// mail). They leave the live triage queue but are kept on record, so this query lets a triager see
// what was discarded and restore one back into triage if it was dismissed by mistake. Paged
// server-side, newest-discarded first, exactly like ListOpenIntake.
public sealed record ListDiscardedIntake(int Skip = 0, int Take = 25) : IQuery<PagedResult<IntakeEmail>>;
