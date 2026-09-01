using Jewel.JPMS.Contracts.Platform;

namespace Jewel.JPMS.Api.Features.Platform.Commands;

public sealed class PublishAppVersionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly PublishAppVersionAuthorisation authorisation;
    private readonly PublishAppVersionValidation validation;
    private readonly ICommandHandler<PublishAppVersion, AnnouncedAppVersion> handler;

    public PublishAppVersionEndpoint(
        SignedInUserResolver users,
        PublishAppVersionAuthorisation authorisation,
        PublishAppVersionValidation validation,
        ICommandHandler<PublishAppVersion, AnnouncedAppVersion> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(PublishAppVersion))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "system/version/publish")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        // PublishedBy is stamped here from the resolved caller — never trusted from the client.
        var command = new PublishAppVersion(signedInUser.Email);
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
