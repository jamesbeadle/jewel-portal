using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.BuildingControl.Commands;

/// <summary>The inspection register's HTTP surface: add on the case route; edit, status and
/// delete on the inspection's own route.</summary>
public sealed class BuildingControlInspectionEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AddBuildingControlInspectionAuthorisation addAuthorisation;
    private readonly AddBuildingControlInspectionValidation addValidation;
    private readonly ICommandHandler<AddBuildingControlInspection, BuildingControlInspection> add;
    private readonly UpdateBuildingControlInspectionAuthorisation updateAuthorisation;
    private readonly UpdateBuildingControlInspectionValidation updateValidation;
    private readonly ICommandHandler<UpdateBuildingControlInspection, BuildingControlInspection> update;
    private readonly SetBuildingControlInspectionStatusAuthorisation statusAuthorisation;
    private readonly ICommandHandler<SetBuildingControlInspectionStatus, BuildingControlInspection> setStatus;
    private readonly DeleteBuildingControlInspectionAuthorisation deleteAuthorisation;
    private readonly ICommandHandler<DeleteBuildingControlInspection, Acknowledgement> delete;

    public BuildingControlInspectionEndpoints(
        SignedInUserResolver users,
        AddBuildingControlInspectionAuthorisation addAuthorisation,
        AddBuildingControlInspectionValidation addValidation,
        ICommandHandler<AddBuildingControlInspection, BuildingControlInspection> add,
        UpdateBuildingControlInspectionAuthorisation updateAuthorisation,
        UpdateBuildingControlInspectionValidation updateValidation,
        ICommandHandler<UpdateBuildingControlInspection, BuildingControlInspection> update,
        SetBuildingControlInspectionStatusAuthorisation statusAuthorisation,
        ICommandHandler<SetBuildingControlInspectionStatus, BuildingControlInspection> setStatus,
        DeleteBuildingControlInspectionAuthorisation deleteAuthorisation,
        ICommandHandler<DeleteBuildingControlInspection, Acknowledgement> delete)
    {
        this.users = users;
        this.addAuthorisation = addAuthorisation;
        this.addValidation = addValidation;
        this.add = add;
        this.updateAuthorisation = updateAuthorisation;
        this.updateValidation = updateValidation;
        this.update = update;
        this.statusAuthorisation = statusAuthorisation;
        this.setStatus = setStatus;
        this.deleteAuthorisation = deleteAuthorisation;
        this.delete = delete;
    }

    [Function(nameof(AddBuildingControlInspection))]
    public async Task<IActionResult> Add(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/cases/{caseId}/inspections")] HttpRequest request,
        string caseId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<AddBuildingControlInspection>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An inspection body is required.");
        var command = posted with { BuildingControlCaseId = caseId, RaisedByEmail = signedInUser.Email };

        if (!addAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = addValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await add.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(UpdateBuildingControlInspection))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "building-control/inspections/{inspectionId}")] HttpRequest request,
        string inspectionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateBuildingControlInspection>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An inspection body is required.");
        var command = posted with { BuildingControlInspectionId = inspectionId };

        if (!updateAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = updateValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await update.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(SetBuildingControlInspectionStatus))]
    public async Task<IActionResult> SetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "building-control/inspections/{inspectionId}/status")] HttpRequest request,
        string inspectionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<SetBuildingControlInspectionStatus>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A status body is required.");
        var command = posted with { BuildingControlInspectionId = inspectionId };

        if (!statusAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await setStatus.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(DeleteBuildingControlInspection))]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "building-control/inspections/{inspectionId}")] HttpRequest request,
        string inspectionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new DeleteBuildingControlInspection(inspectionId);
        if (!deleteAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await delete.HandleAsync(command, cancellationToken));
    }
}
