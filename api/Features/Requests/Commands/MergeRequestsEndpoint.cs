using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// POST /api/requests/{requestId}/merge — merge another General request into this one (the route's
/// request is the survivor). Body: { "mergedRequestId": "..." }. Returns the survivor with the
/// combined description; the merged-away request is closed and stamped with the audit link.
/// </summary>
public sealed class MergeRequestsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly MergeRequestsAuthorisation authorisation;
    private readonly MergeRequestsValidation validation;
    private readonly ICommandHandler<MergeRequests, Request> handler;

    public MergeRequestsEndpoint(
        SignedInUserResolver users,
        MergeRequestsAuthorisation authorisation,
        MergeRequestsValidation validation,
        ICommandHandler<MergeRequests, Request> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(MergeRequests))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "requests/{requestId}/merge")] HttpRequest request,
        string requestId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<MergeRequests>();
        if (body is null) return new BadRequestResult();

        var command = body with { SurvivorRequestId = requestId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
