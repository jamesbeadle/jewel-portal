using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>The office's Received list: nothing still to handle ever drops off it, however far down it has sunk.</summary>
public sealed class ListFormSubmissionsHandler : IQueryHandler<ListFormSubmissions, IReadOnlyList<FormSubmission>>
{
    private const int MostShown = 500;
    private readonly JpmsContext context;

    public ListFormSubmissionsHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<FormSubmission>> HandleAsync(ListFormSubmissions query, CancellationToken cancellationToken)
    {
        var submissions = query.FormFolderId is { } formFolderId
            ? await InFolderAsync(formFolderId, cancellationToken)
            : await UnhandledAndRecentAsync(cancellationToken);
        return submissions.OrderByDescending(row => row.SubmittedAt).Select(row => row.ToModel()).ToList();
    }

    private Task<List<FormSubmissionEntity>> InFolderAsync(string formFolderId, CancellationToken cancellationToken) =>
        context.FormSubmissions.AsNoTracking().Where(row => row.FormFolderId == formFolderId).ToListAsync(cancellationToken);

    private async Task<List<FormSubmissionEntity>> UnhandledAndRecentAsync(CancellationToken cancellationToken)
    {
        var stillToHandle = await context.FormSubmissions.AsNoTracking()
            .Where(row => row.Status == (int)FormSubmissionStatus.New || row.Status == (int)FormSubmissionStatus.InProgress)
            .ToListAsync(cancellationToken);
        var newest = await context.FormSubmissions.AsNoTracking()
            .OrderByDescending(row => row.SubmittedAt).Take(MostShown).ToListAsync(cancellationToken);
        return stillToHandle.Concat(newest).DistinctBy(row => row.FormSubmissionId).ToList();
    }
}

/// <summary>One form's answers and files. The endpoint has already checked the reader against the form's store.</summary>
public sealed class OpenFormSubmissionHandler : IQueryHandler<OpenFormSubmission, FormSubmissionView>
{
    private readonly JpmsContext context;

    public OpenFormSubmissionHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<FormSubmissionView> HandleAsync(OpenFormSubmission query, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.AsNoTracking()
            .FirstOrDefaultAsync(row => row.FormSubmissionId == query.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        var uploads = await context.FormUploads.AsNoTracking()
            .Where(row => row.FormSubmissionId == query.FormSubmissionId).ToListAsync(cancellationToken);
        return FormSubmissionReading.ViewOf(submission, uploads);
    }
}
