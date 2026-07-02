using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Todos;

// Client routes for project to-do items. Mirrors the api endpoints in Features/Todos: list + add are
// project-scoped, update/delete address the item, and create-from-message is the triage-stage path
// that captures several items from one email (tagging the email per item).
public static class TodosRouteRegistration
{
    public static void RegisterTodosRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListTodoItemsForProject, IReadOnlyList<TodoItem>>(
            new QueryRoute("/api/projects/{projectId}/todos",
                query => $"/api/projects/{((ListTodoItemsForProject)query).ProjectId}/todos"));

        commands.Register<AddTodoItem, TodoItem>(
            new CommandRoute("POST", "/api/projects/{projectId}/todos",
                command => $"/api/projects/{((AddTodoItem)command).ProjectId}/todos"));

        commands.Register<UpdateTodoItem, TodoItem>(
            new CommandRoute("PUT", "/api/todo-items/{todoItemId}",
                command => $"/api/todo-items/{((UpdateTodoItem)command).TodoItemId}"));

        commands.Register<DeleteTodoItem, Acknowledgement>(
            new CommandRoute("DELETE", "/api/todo-items/{todoItemId}",
                command => $"/api/todo-items/{((DeleteTodoItem)command).TodoItemId}"));

        commands.Register<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>>(
            new CommandRoute("POST", "/api/mailbox/message/create-todos", _ => "/api/mailbox/message/create-todos"));
    }
}
