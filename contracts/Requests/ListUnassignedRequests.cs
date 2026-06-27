using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Requests that aren't tied to a live project — a blank project id, or one that no longer matches
// any project. These never appear in a project's register, so this query surfaces them for triagers
// to recover (return to triage and re-process). A safety net for stranded requests.
public sealed record ListUnassignedRequests() : IQuery<IReadOnlyList<Request>>;
