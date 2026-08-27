using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.BuildingControl.Commands;

/// <summary>The case's HTTP surface: set up on the project route, edit and move status on the
/// case's own route — mirroring the calendar-event endpoints' shape.</summary>
public sealed class BuildingControlCaseEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly CreateBuildingControlCaseAuthorisation createAuthorisation;
    private readonly CreateBuildingControlCaseValidation createValidation;
    private readonly ICommandHandler<CreateBuildingControlCase, BuildingControlCase> create;
    private readonly UpdateBuildingControlCaseAuthorisation updateAuthorisation;
    private readonly UpdateBuildingControlCaseValidation updateValidation;
    private readonly ICommandHandler<UpdateBuildingControlCase, BuildingControlCase> update;
    private readonly SetBuildingControlCaseStatusAuthorisation statusAuthorisation;
    private readonly ICommandHandler<SetBuildingControlCaseStatus, BuildingControlCase> setStatus;

    public BuildingControlCaseEndpoints(
        SignedInUserResolver users,
        CreateBuildingControlCaseAuthorisation createAuthorisation,
        CreateBuildingControlCaseValidation createValidation,
        ICommandHandler<CreateBuildingControlCase, BuildingControlCase> create,
        UpdateBuildingControlCaseAuthorisation updateAuthorisation,
        UpdateBuildingControlCaseValidation updateValidation,
        ICommandHandler<UpdateBuildingControlCase, BuildingControlCase> update,
        SetBuildingControlCaseStatusAuthorisation statusAuthorisation,
        ICommandHandler<SetBuildingControlCaseStatus, BuildingControlCase> setStatus)
    {
        this.users = users;
        this.createAuthorisation = createAuthorisation;
        this.createValidation = createValidation;
        this.create = create;
        this.updateAuthorisation = updateAuthorisation;
        this.updateValidation = updateValidation;
        this.update = update;
        this.statusAuthorisation = statusAuthorisation;
        this.setStatus = setStatus;
    }

    [Function(nameof(CreateBuildingControlCase))]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/building-control/case")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateBuildingControlCase>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A building control case body is required.");

        // The creator is always the signed-in user — never trusted from the client body — and the
        // project is the route's, whatever the body claimed.
        var command = posted with { ProjectId = projectId, CreatedByEmail = signedInUser.Email };

        if (!createAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = createValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await create.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(UpdateBuildingControlCase))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "building-control/cases/{caseId}")] HttpRequest request,
        string caseId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateBuildingControlCase>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A building control case body is required.");
        var command = posted with { BuildingControlCaseId = caseId };

        if (!updateAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = updateValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await update.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(SetBuildingControlCaseStatus))]
    public async Task<IActionResult> SetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/cases/{caseId}/status")] HttpRequest request,
        string caseId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetBuildingControlCaseStatus>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A status body is required.");
        var command = posted with { BuildingControlCaseId = caseId };

        if (!statusAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await setStatus.HandleAsync(command, cancellationToken));
    }
}
