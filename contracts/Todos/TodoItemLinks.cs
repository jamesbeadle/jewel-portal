using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Todos;

// ---- Linked to-dos -----------------------------------------------------------------------------
//
// A LINK between two to-do items is a flat, two-way "these belong together" association — no
// hierarchy, no completion rules, purely context and navigation. Either item shows the other in
// its detail modal's "Linked to-dos" section. Links are made when a new item is drafted in the
// Control Centre (TodoItemDraft.LinkedTodoItemIds) or afterwards from the detail modal; a link
// names items by id, so it survives either item being moved between projects.

// Link two existing items. The pair is undirected — linking A→B and B→A are the same link, stored
// once — and re-linking an already-linked pair is a quiet no-op, never an error. Gate: the to-do
// manage roles (TodoRoles.AllowedToManageTodos).
// LinkedByEmail is stamped from the signed-in user server-side — never trusted from the client body.
public sealed record LinkTodoItems(
    string TodoItemId,
    string LinkedTodoItemId,
    string LinkedByEmail = "") : ICommand<Acknowledgement>;

// Remove the link between two items (either order). Unlinking a pair that isn't linked is a quiet
// no-op. Same gate as LinkTodoItems.
public sealed record UnlinkTodoItems(
    string TodoItemId,
    string LinkedTodoItemId) : ICommand<Acknowledgement>;

// The items linked to one item — both directions of the undirected pair — in the canonical list
// order. The detail modal's "Linked to-dos" section. Internal-only read, like the item lists.
public sealed record ListLinkedTodoItems(string TodoItemId) : IQuery<IReadOnlyList<TodoItem>>;

// The pool a "link to a to-do" picker offers: every item on the given project PLUS the general
// (company-wide, blank-project) items — the same scope a Control Centre draft can land on. Blank/
// null ProjectId = the company-wide items alone. Open items lead, done items follow (canonical
// list order), so the picker reads like the list pages. Gate: the manage roles, because the only
// thing the pool feeds is making links.
public sealed record ListTodoLinkCandidates(string? ProjectId = null) : IQuery<IReadOnlyList<TodoItem>>;
