namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// POST /api/projects/{projectId}/progress/whatsapp-week/apply — the same multipart form as the
/// preview plus <c>days</c>, the days the person ticked. Reads the export again (the same text
/// always plans the same week), refuses a day that already holds an update, and writes one
/// progress update per chosen day with its photographs.
/// </summary>
public sealed class ApplyWhatsAppWeekEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly WhatsAppWeekAuthorisation authorisation;
    private readonly WhatsAppWeekReader reader;
    private readonly WhatsAppWeekWriter writer;

    public ApplyWhatsAppWeekEndpoint(
        SignedInUserResolver users, JpmsContext context, WhatsAppWeekAuthorisation authorisation,
        WhatsAppWeekReader reader, WhatsAppWeekWriter writer)
    {
        this.users = users;
        this.context = context;
        this.authorisation = authorisation;
        this.reader = reader;
        this.writer = writer;
    }

    [Function(nameof(ApplyWhatsAppWeekEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/progress/whatsapp-week/apply")] HttpRequest request,
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
        var refusal = WhatsAppWeekWriter.Refusal(reading, form.Days);
        if (refusal is not null) return new ConflictObjectResult(refusal);

        try
        {
            return new OkObjectResult(await writer.WriteAsync(projectId, reading, form.Days, signedInUser.Email, cancellationToken));
        }
        catch (InvalidOperationException guard)
        {
            return new BadRequestObjectResult(guard.Message);
        }
    }
}
