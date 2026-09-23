using Jewel.JPMS.Api.Features.Forms.Office;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>The registers a form feeds: a certificate accepted onto training, a workstation NO closed, a licence checked.</summary>
public sealed class FormRegisterEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly FormRegisterValidations validations;
    private readonly ICommandHandler<AcceptTrainingCertificate, TrainingRecord> accept;
    private readonly ICommandHandler<SetTrainingRecordDetails, TrainingRecord> setDetails;
    private readonly ICommandHandler<ResolveWorkstationAction, WorkstationAction> resolve;
    private readonly ICommandHandler<RecordDrivingLicenceCheck, DrivingLicenceCheck> recordCheck;

    public FormRegisterEndpoints(
        SignedInUserResolver users, FormRegisterValidations validations,
        ICommandHandler<AcceptTrainingCertificate, TrainingRecord> accept,
        ICommandHandler<SetTrainingRecordDetails, TrainingRecord> setDetails,
        ICommandHandler<ResolveWorkstationAction, WorkstationAction> resolve,
        ICommandHandler<RecordDrivingLicenceCheck, DrivingLicenceCheck> recordCheck)
    {
        this.users = users;
        this.validations = validations;
        this.accept = accept;
        this.setDetails = setDetails;
        this.resolve = resolve;
        this.recordCheck = recordCheck;
    }

    [Function(nameof(AcceptTrainingCertificate))]
    public async Task<IActionResult> Accept([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "training-records")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<AcceptTrainingCertificate>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say whose certificate, and for which course.");
        var command = posted with { AcceptedByEmail = signedInUser.Email };
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(accept, command, validations.Accept.Check(command), cancellationToken);
    }

    [Function(nameof(SetTrainingRecordDetails))]
    public async Task<IActionResult> SetDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "training-records/{trainingRecordId}")] HttpRequest request,
        string trainingRecordId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SetTrainingRecordDetails>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say what changes.");
        var command = posted with { TrainingRecordId = trainingRecordId };
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(setDetails, command, validations.Details.Check(command), cancellationToken);
    }

    [Function(nameof(ResolveWorkstationAction))]
    public async Task<IActionResult> Resolve(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "workstation-actions/{workstationActionId}/resolve")] HttpRequest request,
        string workstationActionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<ResolveWorkstationAction>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say whether it is fixed or accepted.");
        var command = posted with { WorkstationActionId = workstationActionId, ResolvedByEmail = signedInUser.Email };
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(resolve, command, validations.Resolve.Check(command), cancellationToken);
    }

    [Function(nameof(RecordDrivingLicenceCheck))]
    public async Task<IActionResult> RecordCheck(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "driving-licence-checks")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<RecordDrivingLicenceCheck>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say what the DVLA record showed.");
        var command = posted with { CheckedByEmail = signedInUser.Email };
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(recordCheck, command, validations.LicenceCheck.Check(command), cancellationToken);
    }
}

/// <summary>The four register commands' validations, handed to their endpoints together.</summary>
public sealed record FormRegisterValidations(
    AcceptTrainingCertificateValidation Accept, SetTrainingRecordDetailsValidation Details,
    ResolveWorkstationActionValidation Resolve, RecordDrivingLicenceCheckValidation LicenceCheck);
