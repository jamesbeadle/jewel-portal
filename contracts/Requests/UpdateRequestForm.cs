using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// One row of the official document's itemised-queries table, as edited on the request page.
/// No id or position: the command replaces the full list and positions are the 1-based order of
/// this collection, so reordering / deleting in the editor "just works".
/// </summary>
public sealed record RequestItemDraft(
    string DrawingRef,
    string MemberArea,
    string Query,
    string? Response = null);

/// <summary>
/// Saves the structured body of a request's official document (RFI sheet): the itemised queries
/// plus the basis-of-queries, response/action-required and impact-if-late sections. Replace-all
/// semantics for the items — the submitted list becomes the request's items in the given order.
/// The next document render (download, send, draft) picks the new content up automatically.
/// </summary>
public sealed record UpdateRequestForm(
    string RequestId,
    string? BasisOfQueries,
    string? ResponseActionRequired,
    string? ImpactIfLate,
    IReadOnlyList<RequestItemDraft> Items) : ICommand<Request>;
