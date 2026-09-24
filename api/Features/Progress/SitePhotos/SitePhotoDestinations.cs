using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Progress.SitePhotos;

/// <summary>Reads where each filed pool photo went — the project and the update's work day — in
/// two queries for the whole list, so the pool can say "By France, 21 Sep" on every filed card.</summary>
internal static class SitePhotoDestinations
{
    public static async Task<IReadOnlyList<SitePhoto>> ToModelsAsync(
        JpmsContext context, IReadOnlyList<SitePhotoEntity> entities, CancellationToken cancellationToken)
    {
        var updateIds = entities.Select(entity => entity.FiledToProgressUpdateId)
            .OfType<string>().Distinct().ToList();
        var updates = await context.ProgressUpdates.AsNoTracking()
            .Where(update => updateIds.Contains(update.ProgressUpdateId))
            .ToDictionaryAsync(update => update.ProgressUpdateId, cancellationToken);
        var projectIds = updates.Values.Select(update => update.ProjectId).Distinct().ToList();
        var projects = await context.Projects.AsNoTracking()
            .Where(project => projectIds.Contains(project.ProjectId))
            .ToDictionaryAsync(project => project.ProjectId, cancellationToken);

        return entities
            .Select(entity => entity.ToModel() with { FiledTo = DestinationOf(entity, updates, projects) })
            .ToList();
    }

    private static SitePhotoDestination? DestinationOf(
        SitePhotoEntity entity,
        IReadOnlyDictionary<string, ProgressUpdateEntity> updates,
        IReadOnlyDictionary<string, ProjectEntity> projects)
    {
        if (entity.FiledToProgressUpdateId is not { } updateId) return null;
        if (!updates.TryGetValue(updateId, out var update)) return null;
        var project = projects.GetValueOrDefault(update.ProjectId);
        return new SitePhotoDestination(
            project?.Reference ?? "", project?.Name ?? "", update.Title, update.WorkDate);
    }
}
