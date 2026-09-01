using Jewel.JPMS.Api.Features.Closeout.Commands;
using Jewel.JPMS.Api.Features.Drawings.Commands;
using Jewel.JPMS.Api.Features.Progress;
using Jewel.JPMS.Api.Features.Progress.Commands;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Site.Commands;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Contracts.Todos;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class SiteAndProgressActions
{
    private static IEnumerable<AiAction> ToDosActions() => new AiAction[]
    {
        new AiAction(
            Name: "create_todo_items_from_message",
            Area: "To-dos",
            Description: "Creates one or more to-do items from a mailbox message (triage pathway) "
                + "and tags the email \"JPMS/TODO-####\" for every item — the email is the items' "
                + "only record, no copy is stored. A blank projectId makes them company-wide "
                + "general items.",
            CommandType: typeof(CreateTodoItemsFromMessage),
            ResultType: typeof(IReadOnlyList<TodoItem>),
            AuthorisationType: typeof(CreateTodoItemsFromMessageAuthorisation),
            ValidationType: typeof(CreateTodoItemsFromMessageValidation),
            VisibleTo: TriageRoles.AllowedToTriage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "messageId is a mailbox message id from the triage queue, not a request id. "
                + "When linkRequestId is set the email is also tagged to that request first — it "
                + "must exist, be on the same project and not be Closed."),

        new AiAction(
            Name: "delete_todo_item",
            Area: "To-dos",
            Description: "Deletes a to-do item permanently, together with its activity timeline "
                + "and any to-do-to-to-do links naming it. There is no undo.",
            CommandType: typeof(DeleteTodoItem),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteTodoItemAuthorisation),
            ValidationType: typeof(DeleteTodoItemValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which item, by title, before calling. todoItemId comes "
                + "from list_todos or find_by_reference."),

        new AiAction(
            Name: "link_todo_items",
            Area: "To-dos",
            Description: "Links two to-do items so each lists the other as related work. Changes "
                + "how the work reads on the To-dos pages; nothing else on either item moves.",
            CommandType: typeof(LinkTodoItems),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(LinkTodoItemsAuthorisation),
            ValidationType: typeof(LinkTodoItemsValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: new[] { "LinkedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "Both ids come from list_todos or find_by_reference."),

        new AiAction(
            Name: "move_todo_item",
            Area: "To-dos",
            Description: "Re-files a to-do item under a different project (or company-wide with a "
                + "blank projectId) and touches nothing else — assignee, due date, linked emails "
                + "and open/done state all stay as they were.",
            CommandType: typeof(MoveTodoItem),
            ResultType: typeof(TodoItem),
            AuthorisationType: typeof(MoveTodoItemAuthorisation),
            ValidationType: typeof(MoveTodoItemValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "Moving to COMPANY-WIDE (blank projectId) is narrower — managing director and "
                + "administrators only. Further per-record checks apply at execution."),

        new AiAction(
            Name: "unlink_todo_items",
            Area: "To-dos",
            Description: "Removes the link between two to-do items. The items themselves are "
                + "untouched.",
            CommandType: typeof(UnlinkTodoItems),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(UnlinkTodoItemsAuthorisation),
            ValidationType: typeof(UnlinkTodoItemsValidation),
            VisibleTo: TodoRoles.AllowedToManageTodos,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        // ── Site (site reports) ───────────────────────────────────────────────────────────────

    };
}
