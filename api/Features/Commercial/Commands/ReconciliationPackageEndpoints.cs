using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>The three package commands share one endpoint class — same shape, same
/// authorisation surface, same friendly-rejection handling.</summary>
public sealed class ReconciliationPackageEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly ReconciliationPackageAuthorisation authorisation;
    private readonly SaveReconciliationPackageValidation saveValidation;
    private readonly ICommandHandler<SaveReconciliationPackage, ReconciliationPackage> saveHandler;
    private readonly ICommandHandler<RemoveReconciliationPackage, Acknowledgement> removeHandler;
    private readonly ICommandHandler<SetReconciliationPackageLock, ReconciliationPackage> lockHandler;

    public ReconciliationPackageEndpoints(
        SignedInUserResolver users,
        ReconciliationPackageAuthorisation authorisation,
        SaveReconciliationPackageValidation saveValidation,
        ICommandHandler<SaveReconciliationPackage, ReconciliationPackage> saveHandler,
        ICommandHandler<RemoveReconciliationPackage, Acknowledgement> removeHandler,
        ICommandHandler<SetReconciliationPackageLock, ReconciliationPackage> lockHandler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.saveValidation = saveValidation;
        this.saveHandler = saveHandler;
        this.removeHandler = removeHandler;
        this.lockHandler = lockHandler;
    }

    [Function(nameof(SaveReconciliationPackage))]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/reconciliation-packages")] HttpRequest request,
        string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SaveReconciliationPackage>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId) return new BadRequestObjectResult("Route projectId does not match body.");

        // Not ForbidResult: executing it needs a registered authentication scheme, and this app's
        // cookie-session auth has none — it throws, the function 500s, and Static Web Apps hands
        // the client an opaque "Backend call failure". Return the 403 with a showable message.
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to manage packages.")
            { StatusCode = StatusCodes.Status403Forbidden };

        var validationOutcome = saveValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await saveHandler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(RemoveReconciliationPackage))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "projects/{projectId}/reconciliation-packages/{packageId}")] HttpRequest request,
        string projectId,
        string packageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new RemoveReconciliationPackage(projectId, packageId);
        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to manage packages.")
            { StatusCode = StatusCodes.Status403Forbidden };

        try
        {
            return new OkObjectResult(await removeHandler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(SetReconciliationPackageLock))]
    public async Task<IActionResult> Lock(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/reconciliation-packages/{packageId}/lock")] HttpRequest request,
        string projectId,
        string packageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = await request.ReadFromJsonAsync<SetReconciliationPackageLock>();
        if (command is null) return new BadRequestResult();
        if (command.ProjectId != projectId || command.ReconciliationPackageId != packageId)
            return new BadRequestObjectResult("Route does not match body.");

        if (!authorisation.Allows(signedInUser, command))
            return new ObjectResult("Your role doesn't have permission to manage packages.")
            { StatusCode = StatusCodes.Status403Forbidden };

        try
        {
            return new OkObjectResult(await lockHandler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
