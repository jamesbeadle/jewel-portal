using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.ProjectContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// DELETE /api/projects/{projectId}/contract/amendments/{amendmentId} — permanently removes one
/// amendment and its stored document. Same narrow role set that manages the contract terms.
/// </summary>
public sealed class RemoveProjectContractAmendmentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly RemoveProjectContractAmendmentAuthorisation authorisation;
    private readonly RemoveProjectContractAmendmentValidation validation;
    private readonly ICommandHandler<RemoveProjectContractAmendment, Acknowledgement> handler;

    public RemoveProjectContractAmendmentEndpoint(
        SignedInUserResolver users,
        RemoveProjectContractAmendmentAuthorisation authorisation,
        RemoveProjectContractAmendmentValidation validation,
        ICommandHandler<RemoveProjectContractAmendment, Acknowledgement> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(RemoveProjectContractAmendment))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projects/{projectId}/contract/amendments/{amendmentId}")] HttpRequest request,
        string projectId,
        string amendmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveProjectContractAmendment(projectId, amendmentId);

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
