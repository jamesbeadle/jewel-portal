using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos;

internal static class TodosEntityMapping
{
    public static TodoItem ToModel(this TodoItemEntity entity) =>
        new(entity.TodoItemId,
            entity.ProjectId,
            entity.Reference,
            entity.Title,
            entity.Notes,
            entity.AssigneeEmail,
            entity.CreatedByEmail,
            entity.IsComplete,
            entity.CreatedAt,
            entity.DueAt,
            entity.CompletedAt);
}
