using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ProjectContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>PUT /api/projects/{projectId}/contract — record or replace the contract terms.</summary>
public sealed class SetProjectContractTermsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetProjectContractTermsAuthorisation authorisation;
    private readonly SetProjectContractTermsValidation validation;
    private readonly ICommandHandler<SetProjectContractTerms, ProjectContract> handler;

    public SetProjectContractTermsEndpoint(
        SignedInUserResolver users,
        SetProjectContractTermsAuthorisation authorisation,
        SetProjectContractTermsValidation validation,
        ICommandHandler<SetProjectContractTerms, ProjectContract> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetProjectContractTerms))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}/contract")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SetProjectContractTerms>();
        if (body is null) return new BadRequestResult();

        // Route id and caller identity are re-stamped — never trusted from the body.
        var command = body with { ProjectId = projectId, UpdatedByEmail = signedInUser.Email };

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new[] { ex.Message });
        }
    }
}
