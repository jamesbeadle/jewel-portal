using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// PUT /api/requests/{requestId}/form — save the structured body of the request's official document
/// (itemised queries + narrative sections). Body: the <see cref="UpdateRequestForm"/> command.
/// </summary>
public sealed class UpdateRequestFormEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly UpdateRequestFormAuthorisation authorisation;
    private readonly UpdateRequestFormValidation validation;
    private readonly ICommandHandler<UpdateRequestForm, Request> handler;

    public UpdateRequestFormEndpoint(
        SignedInUserResolver users,
        JpmsContext context,
        UpdateRequestFormAuthorisation authorisation,
        UpdateRequestFormValidation validation,
        ICommandHandler<UpdateRequestForm, Request> handler)
    {
        this.users = users;
        this.context = context;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(UpdateRequestForm))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "requests/{requestId}/form")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<UpdateRequestForm>();
        if (body is null) return new BadRequestResult();

        var command = body with { RequestId = requestId };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        if (!await RequestScope.MayActOnAsync(context, signedInUser, requestId, cancellationToken))
            return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
