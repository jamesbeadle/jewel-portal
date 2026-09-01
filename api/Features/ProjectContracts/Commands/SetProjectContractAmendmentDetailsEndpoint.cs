using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// PUT /api/projects/{projectId}/contract/amendments/{amendmentId} — correct an amendment's title,
/// date or notes. The document is untouched here, by design.
/// </summary>
public sealed class SetProjectContractAmendmentDetailsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetProjectContractAmendmentDetailsAuthorisation authorisation;
    private readonly SetProjectContractAmendmentDetailsValidation validation;
    private readonly ICommandHandler<SetProjectContractAmendmentDetails, ProjectContractAmendment> handler;

    public SetProjectContractAmendmentDetailsEndpoint(
        SignedInUserResolver users,
        SetProjectContractAmendmentDetailsAuthorisation authorisation,
        SetProjectContractAmendmentDetailsValidation validation,
        ICommandHandler<SetProjectContractAmendmentDetails, ProjectContractAmendment> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(SetProjectContractAmendmentDetails))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "projects/{projectId}/contract/amendments/{amendmentId}")] HttpRequest request,
        string projectId,
        string amendmentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var body = await request.ReadFromJsonAsync<SetProjectContractAmendmentDetails>();
        if (body is null) return new BadRequestResult();

        // Route ids and caller identity are re-stamped — never trusted from the body.
        var command = body with
        {
            ProjectId = projectId,
            ProjectContractAmendmentId = amendmentId,
            UpdatedByEmail = signedInUser.Email
        };

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
