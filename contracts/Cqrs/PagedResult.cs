namespace Jewel.JPMS.Contracts.Cqrs;

// A single page of results plus the total number of matching rows, so a client can render
// "showing N of Total" and drive prev/next without fetching everything. Skip/Take echo back
// the (clamped) paging that was actually applied by the server.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Skip, int Take);
