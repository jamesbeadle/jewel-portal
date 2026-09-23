using Jewel.JPMS.Api.Features.Forms.Office;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>Reading the registers the forms feed: training, the workstation work queue, and the licence checks.</summary>
public sealed class FormRegisterReadEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListTrainingRecords, IReadOnlyList<TrainingRecord>> training;
    private readonly IQueryHandler<ListWorkstationActions, IReadOnlyList<WorkstationAction>> workstations;
    private readonly IQueryHandler<ListDrivingLicenceChecks, IReadOnlyList<DrivingLicenceCheck>> licences;

    public FormRegisterReadEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListTrainingRecords, IReadOnlyList<TrainingRecord>> training,
        IQueryHandler<ListWorkstationActions, IReadOnlyList<WorkstationAction>> workstations,
        IQueryHandler<ListDrivingLicenceChecks, IReadOnlyList<DrivingLicenceCheck>> licences)
    {
        this.users = users;
        this.training = training;
        this.workstations = workstations;
        this.licences = licences;
    }

    [Function(nameof(ListTrainingRecords))]
    public async Task<IActionResult> Training([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "training-records")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(training, new ListTrainingRecords(), request.HttpContext.RequestAborted);
    }

    [Function(nameof(ListWorkstationActions))]
    public async Task<IActionResult> Workstations(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "workstation-actions")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(workstations, new ListWorkstationActions(), request.HttpContext.RequestAborted);
    }

    [Function(nameof(ListDrivingLicenceChecks))]
    public async Task<IActionResult> Licences(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "driving-licence-checks")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(licences, new ListDrivingLicenceChecks(), request.HttpContext.RequestAborted);
    }
}
