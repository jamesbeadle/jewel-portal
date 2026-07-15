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
        services.AddScoped<IQueryHandler<ListTodoAssignees, IReadOnlyList<DirectoryUser>>, ListTodoAssigneesHandler>();

        services.AddScoped<ICommandHandler<AddTodoItem, TodoItem>, AddTodoItemHandler>();
        services.AddScoped<AddTodoItemAuthorisation>();
        services.AddScoped<AddTodoItemValidation>();

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
