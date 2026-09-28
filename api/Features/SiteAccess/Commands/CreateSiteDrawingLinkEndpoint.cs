using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Contracts.SiteAccess;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

/// <summary>
/// POST /api/projects/{projectId}/site-links — mints a QR poster's link for one folder. The author
/// and the public site origin are the request's, never the body's; the answer is the only copy of
/// the URL, the QR code and the poster.
/// </summary>
public sealed class CreateSiteDrawingLinkEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IConfiguration configuration;
    private readonly CreateSiteDrawingLinkAuthorisation authorisation;
    private readonly CreateSiteDrawingLinkValidation validation;
    private readonly ICommandHandler<CreateSiteDrawingLink, SiteDrawingLinkCreated> handler;

    public CreateSiteDrawingLinkEndpoint(
        SignedInUserResolver users,
        IConfiguration configuration,
        CreateSiteDrawingLinkAuthorisation authorisation,
        CreateSiteDrawingLinkValidation validation,
        ICommandHandler<CreateSiteDrawingLink, SiteDrawingLinkCreated> handler)
    {
        this.users = users;
        this.configuration = configuration;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateSiteDrawingLink))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/site-links")] HttpRequest request,
        string projectId)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateSiteDrawingLink>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A site link body is required.");
        var siteOrigin = SiteBaseUrl.Resolve(configuration, request);
        var command = posted with { ProjectId = projectId, CreatedByEmail = signedInUser.Email, SiteOrigin = siteOrigin };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException guard)
        {
            return new BadRequestObjectResult(guard.Message);
        }
    }
}
