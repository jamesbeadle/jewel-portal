using Jewel.JPMS.Api.Features.Progress.Photos;
using Jewel.JPMS.Contracts.Hs;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Hs.Thread.Commands;

/// <summary>
/// POST /api/hs-records/{hsRecordId}/comments/with-photos — multipart/form-data: a "text" field and
/// the photographs of the work done (JPEG, PNG, HEIC). The site manager's door from the page: words,
/// pictures or both; a comment with pictures and no words says so for him. One save — the comment,
/// its event and its photographs land together.
/// </summary>
public sealed class CommentWithPhotosEndpoint
{
    private const string TextField = "text";
    private const string PhotographsOnly = "Photograph(s) of the work done.";

    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly CommentOnHsRecordAuthorisation authorisation;
    private readonly CommentOnHsRecordValidation validation;
    private readonly CommentOnHsRecordHandler comments;
    private readonly HsRecordPhotoIntake intake;

    public CommentWithPhotosEndpoint(
        SignedInUserResolver users, JpmsContext context, CommentOnHsRecordAuthorisation authorisation,
        CommentOnHsRecordValidation validation, CommentOnHsRecordHandler comments, HsRecordPhotoIntake intake)
    {
        this.users = users;
        this.context = context;
        this.authorisation = authorisation;
        this.validation = validation;
        this.comments = comments;
        this.intake = intake;
    }

    [Function(nameof(CommentWithPhotosEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hs-records/{hsRecordId}/comments/with-photos")] HttpRequest request,
        string hsRecordId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var read = await ProgressPhotoFormReader.ReadAsync(form, cancellationToken);
        var hasFiles = form.Files.Any(file => file.Length > 0);
        if (hasFiles && read.Refusal is not null) return new BadRequestObjectResult(read.Refusal);
        var hasPhotographs = read.Images.Count > 0;
        var text = form[TextField].ToString().Trim();
        var words = text.Length == 0 && hasPhotographs ? PhotographsOnly : text;
        var command = new CommentOnHsRecord(hsRecordId, words, signedInUser.Email, signedInUser.DisplayName);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await SaveAsync(command, read.Images, cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(CommentOnHsRecord command, IReadOnlyList<IncomingProgressPhoto> images, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            var comment = await comments.StageAsync(command, now, cancellationToken);
            var outcomes = await intake.TakeAsync(comment, images, now, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            var photos = await context.HsRecordPhotos.AsNoTracking()
                .Where(row => row.HsRecordCommentId == comment.HsRecordCommentId).ToListAsync(cancellationToken);
            return new OkObjectResult(new HsRecordCommentWithPhotos(comment.ToModel(photos), outcomes));
        }
        catch (InvalidOperationException refusal) { return new ConflictObjectResult(refusal.Message); }
    }
}
