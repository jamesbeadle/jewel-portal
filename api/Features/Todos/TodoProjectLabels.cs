using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Todos;

internal sealed record TodoProjectLabel(string Reference, string Name);

internal static class TodoProjectLabels
{
    // One saved or fetched item, labelled with its pinned person and its project in one go.
    public static async Task<TodoItem> ToModelAsync(
        this JpmsContext context, TodoItemEntity entity, CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? aboutReferences = null)
    {
        var personNames = await context.PersonNamesForAsync(new[] { entity }, cancellationToken);
        var projectLabels = await context.ProjectLabelsForAsync(new[] { entity }, cancellationToken);
        return entity.ToModel(personNames, aboutReferences, projectLabels);
    }

    public static async Task<IReadOnlyDictionary<string, TodoProjectLabel>> ProjectLabelsForAsync(
        this JpmsContext context, IEnumerable<TodoItemEntity> entities, CancellationToken cancellationToken)
    {
        var projectIds = entities
            .Select(entity => entity.ProjectId)
            .Where(projectId => !string.IsNullOrWhiteSpace(projectId))
            .Distinct()
            .ToList();
        if (projectIds.Count == 0) return new Dictionary<string, TodoProjectLabel>();

        var projects = await context.Projects.AsNoTracking()
            .Where(project => projectIds.Contains(project.ProjectId))
            .Select(project => new { project.ProjectId, project.Reference, project.Name })
            .ToListAsync(cancellationToken);

        return projects.ToDictionary(
            project => project.ProjectId,
            project => new TodoProjectLabel(project.Reference, project.Name));
    }
}
