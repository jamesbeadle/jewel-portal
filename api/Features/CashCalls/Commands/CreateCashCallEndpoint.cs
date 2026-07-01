using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

/// <summary>
/// POST /api/projects/{projectId}/cash-calls — raise a monthly cash call. Body: { periodMonth,
/// amountRequested, valuationClaimId? }.
/// </summary>
public sealed class CreateCashCallEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CreateCashCallAuthorisation authorisation;
    private readonly CreateCashCallValidation validation;
    private readonly ICommandHandler<CreateCashCall, CashCall> handler;

    public CreateCashCallEndpoint(
        SignedInUserResolver users,
        CreateCashCallAuthorisation authorisation,
        CreateCashCallValidation validation,
        ICommandHandler<CreateCashCall, CashCall> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateCashCall))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/cash-calls")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<CreateCashCall>();
        if (body is null) return new BadRequestResult();

        var command = body with { ProjectId = projectId };

        if (!authorisation.Allows(signedInUser, command)) return new ForbidResult();

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
    }
}
