using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Todos.Commands;
using Jewel.JPMS.Api.Features.Todos.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Todos;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Todos;

public static class TodosFeatureRegistration
{
    public static IServiceCollection AddTodosFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListTodoItemsForProject, IReadOnlyList<TodoItem>>, ListTodoItemsForProjectHandler>();
        services.AddScoped<IQueryHandler<ListTodoAssignableRoles, IReadOnlyList<Role>>, ListTodoAssignableRolesHandler>();

        // The To-dos browser + "My to-dos" dashboard panel: the MD / administrators read everything,
        // everyone else reads the items assigned to them.
        services.AddScoped<IQueryHandler<ListMyTodoItems, IReadOnlyList<TodoItem>>, ListMyTodoItemsHandler>();
        services.AddScoped<IQueryHandler<ListAllTodoItems, IReadOnlyList<TodoItem>>, ListAllTodoItemsHandler>();

        services.AddScoped<ICommandHandler<AddTodoItem, TodoItem>, AddTodoItemHandler>();
        services.AddScoped<AddTodoItemAuthorisation>();
        services.AddScoped<AddTodoItemValidation>();

        // General (company-wide, no-project) items added directly from the To-dos browser page.
        services.AddScoped<ICommandHandler<AddGeneralTodoItem, TodoItem>, AddGeneralTodoItemHandler>();
        services.AddScoped<AddGeneralTodoItemAuthorisation>();
        services.AddScoped<AddGeneralTodoItemValidation>();

        services.AddScoped<ICommandHandler<UpdateTodoItem, TodoItem>, UpdateTodoItemHandler>();
        services.AddScoped<UpdateTodoItemAuthorisation>();
        services.AddScoped<UpdateTodoItemValidation>();

        services.AddScoped<ICommandHandler<DeleteTodoItem, Acknowledgement>, DeleteTodoItemHandler>();
        services.AddScoped<DeleteTodoItemAuthorisation>();
        services.AddScoped<DeleteTodoItemValidation>();

        // Triage: create one or more to-dos from a mailbox message, tagging the email per item.
        services.AddScoped<ICommandHandler<CreateTodoItemsFromMessage, IReadOnlyList<TodoItem>>, CreateTodoItemsFromMessageHandler>();
        services.AddScoped<CreateTodoItemsFromMessageAuthorisation>();
        services.AddScoped<CreateTodoItemsFromMessageValidation>();

        return services;
    }
}
