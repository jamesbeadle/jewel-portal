using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// The people and companies forms are filed under, with the dates their retention runs from — the
/// office records the day an engagement ended and the day a company vehicle came back, and the
/// retention sweep takes it from there.
/// </summary>
public sealed class ListFormFoldersHandler : IQueryHandler<ListFormFolders, IReadOnlyList<FormFolder>>
{
    private readonly JpmsContext context;

    public ListFormFoldersHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<FormFolder>> HandleAsync(ListFormFolders query, CancellationToken cancellationToken)
    {
        var folders = await context.FormFolders.AsNoTracking().OrderBy(row => row.Name).ToListAsync(cancellationToken);
        var counts = await context.FormSubmissions.AsNoTracking()
            .Where(row => row.FormFolderId != null && row.DestroyedAt == null)
            .GroupBy(row => row.FormFolderId!)
            .Select(group => new { FormFolderId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(entry => entry.FormFolderId, entry => entry.Count, cancellationToken);
        return folders.Select(folder => folder.ToModel(counts.GetValueOrDefault(folder.FormFolderId))).ToList();
    }
}
