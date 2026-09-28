using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

/// <summary>POST /api/site-links/{siteDrawingLinkId}/revoke — stops a poster's link at once.</summary>
public sealed class RevokeSiteDrawingLinkEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RevokeSiteDrawingLinkAuthorisation authorisation;
    private readonly RevokeSiteDrawingLinkValidation validation;
    private readonly ICommandHandler<RevokeSiteDrawingLink, Acknowledgement> handler;

    public RevokeSiteDrawingLinkEndpoint(
        SignedInUserResolver users,
        RevokeSiteDrawingLinkAuthorisation authorisation,
        RevokeSiteDrawingLinkValidation validation,
        ICommandHandler<RevokeSiteDrawingLink, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RevokeSiteDrawingLink))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "site-links/{siteDrawingLinkId}/revoke")] HttpRequest request,
        string siteDrawingLinkId)
    {
        var httpContext = request.HttpContext;
        var cancellationToken = httpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RevokeSiteDrawingLink(siteDrawingLinkId, signedInUser.Email);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException guard)
        {
            return new NotFoundObjectResult(guard.Message);
        }
    }
}
