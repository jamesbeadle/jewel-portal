using Jewel.JPMS.Api.Features.Progress.Photos;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

/// <summary>POST /api/my/labour/notes/{progressUpdateId}/photos — the day's photographs from the
/// worker's phone onto the note their own sign-out wrote, as multipart/form-data. A note that is not
/// the caller's own is nothing to them.</summary>
public sealed class MyDayPhotosEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MyDayPhotosHandler handler;
    public MyDayPhotosEndpoint(SignedInUserResolver users, MyDayPhotosHandler handler)
    { this.users = users; this.handler = handler; }

    [Function("MyDayPhotos")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "my/labour/notes/{progressUpdateId}/photos")] HttpRequest request,
        string progressUpdateId)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var read = await ProgressPhotoFormReader.ReadAsync(form, cancellationToken);
        if (read.Refusal is not null) return new BadRequestObjectResult(read.Refusal);

        var note = await handler.OwnNoteAsync(progressUpdateId, signedInUser.Email, cancellationToken);
        if (note is null) return new NotFoundResult();
        var added = await handler.AddAsync(note, signedInUser.Email, read.Images, cancellationToken);
        return added.Failure is not null
            ? new BadRequestObjectResult(added.Failure)
            : new OkObjectResult(added.Batch);
    }
}
