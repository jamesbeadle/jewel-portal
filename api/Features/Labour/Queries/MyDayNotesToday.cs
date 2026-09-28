using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>The notes a worker has already written today, project by project: the progress
/// updates filed in their own name on today's working date, each with its photograph count and
/// the references of the records its log raised, so the day's card can show the log, say what
/// went to the office, and offer to add photographs to it.</summary>
public sealed class MyDayNotesToday
{
    private readonly JpmsContext context;
    public MyDayNotesToday(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyDictionary<string, MyDayNote>> ByProjectAsync(
        string email, IReadOnlyList<string> projectIds, DateTimeOffset today, CancellationToken cancellationToken)
    {
        var notes = await context.ProgressUpdates
            .Where(note => note.CreatedByEmail == email && note.WorkDate == today && projectIds.Contains(note.ProjectId))
            .OrderByDescending(note => note.CreatedAt)
            .Select(note => new { note.ProjectId, note.ProgressUpdateId, note.Title, note.Description })
            .ToListAsync(cancellationToken);
        var noteIds = notes.Select(note => note.ProgressUpdateId).ToList();
        var photoCounts = await context.ProgressPhotos
            .Where(photo => noteIds.Contains(photo.ProgressUpdateId))
            .GroupBy(photo => photo.ProgressUpdateId)
            .Select(group => new { ProgressUpdateId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ProgressUpdateId, group => group.Count, cancellationToken);
        var raised = await MyDayRaisedReferences.ForNotesAsync(context, noteIds, cancellationToken);

        var byProject = new Dictionary<string, MyDayNote>();
        foreach (var note in notes)
        {
            if (byProject.ContainsKey(note.ProjectId)) continue;
            var photoCount = photoCounts.GetValueOrDefault(note.ProgressUpdateId);
            var references = raised.For(note.ProgressUpdateId);
            byProject[note.ProjectId] = new MyDayNote(
                note.ProgressUpdateId, note.Title, note.Description, photoCount, references.SiteInstruction, references.Defect);
        }
        return byProject;
    }
}
