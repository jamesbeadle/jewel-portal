using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// PUT /api/requests/{requestId}/form — save the structured body of the request's official document
/// (itemised queries + narrative sections). Body: the <see cref="UpdateRequestForm"/> command.
/// </summary>
public sealed class UpdateRequestFormEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly UpdateRequestFormAuthorisation authorisation;
    private readonly UpdateRequestFormValidation validation;
    private readonly ICommandHandler<UpdateRequestForm, Request> handler;

    public UpdateRequestFormEndpoint(
        SignedInUserResolver users,
        UpdateRequestFormAuthorisation authorisation,
        UpdateRequestFormValidation validation,
        ICommandHandler<UpdateRequestForm, Request> handler)
    {
        this.users = users;
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

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
