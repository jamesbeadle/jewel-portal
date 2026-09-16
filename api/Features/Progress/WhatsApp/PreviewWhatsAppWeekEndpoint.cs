namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// POST /api/projects/{projectId}/progress/whatsapp-week/preview — multipart/form-data
/// (<see cref="WhatsAppWeekForm"/>). Reads the week and answers what it found, writing nothing:
/// the review screen a person confirms before <see cref="ApplyWhatsAppWeekEndpoint"/>.
/// </summary>
public sealed class PreviewWhatsAppWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly WhatsAppWeekAuthorisation authorisation;
    private readonly WhatsAppWeekReader reader;

    public PreviewWhatsAppWeekEndpoint(
        SignedInUserResolver users, JpmsContext context, WhatsAppWeekAuthorisation authorisation, WhatsAppWeekReader reader)
    {
        this.users = users;
        this.context = context;
        this.authorisation = authorisation;
        this.reader = reader;
    }

    [Function(nameof(PreviewWhatsAppWeekEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/progress/whatsapp-week/preview")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!authorisation.Allows(signedInUser)) return new StatusCodeResult(403);
        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");

        var projectExists = await context.Projects.AnyAsync(row => row.ProjectId == projectId, cancellationToken);
        if (!projectExists) return new NotFoundObjectResult($"Project {projectId} not found.");

        var form = await WhatsAppWeekForm.ReadAsync(await request.ReadFormAsync(cancellationToken), cancellationToken);
        if (form.Refusal is not null) return new BadRequestObjectResult(form.Refusal);

        using var archive = form.Archive!;
        var reading = await reader.ReadAsync(projectId, archive, form.Week!, cancellationToken);
        return new OkObjectResult(reading.ToPreview(projectId));
    }
}
