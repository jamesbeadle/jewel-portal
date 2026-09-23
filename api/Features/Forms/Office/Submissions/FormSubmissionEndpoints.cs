using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// Working a form that came in: its status, filing its certificates to the directory, and a folder's
/// retention dates — each gated by role, then by FormRecordScope on the form or folder it touches.
/// </summary>
public sealed class FormSubmissionEndpoints
{
    private readonly JpmsContext context;
    private readonly SignedInUserResolver users;
    private readonly SetFormSubmissionStatusAuthorisation statusAuthorisation;
    private readonly SetFormSubmissionStatusValidation statusValidation;
    private readonly ICommandHandler<SetFormSubmissionStatus, FormSubmission> setStatus;
    private readonly FileFormToDirectoryAuthorisation fileAuthorisation;
    private readonly FileFormToDirectoryValidation fileValidation;
    private readonly ICommandHandler<FileFormToDirectory, FormDirectoryFiling> fileToDirectory;
    private readonly RecordFormFolderDatesAuthorisation datesAuthorisation;
    private readonly RecordFormFolderDatesValidation datesValidation;
    private readonly ICommandHandler<RecordFormFolderDates, FormFolder> recordDates;

    public FormSubmissionEndpoints(
        JpmsContext context, SignedInUserResolver users,
        SetFormSubmissionStatusAuthorisation statusAuthorisation, SetFormSubmissionStatusValidation statusValidation,
        ICommandHandler<SetFormSubmissionStatus, FormSubmission> setStatus,
        FileFormToDirectoryAuthorisation fileAuthorisation, FileFormToDirectoryValidation fileValidation,
        ICommandHandler<FileFormToDirectory, FormDirectoryFiling> fileToDirectory,
        RecordFormFolderDatesAuthorisation datesAuthorisation, RecordFormFolderDatesValidation datesValidation,
        ICommandHandler<RecordFormFolderDates, FormFolder> recordDates)
    {
        this.context = context;
        this.users = users;
        this.statusAuthorisation = statusAuthorisation;
        this.statusValidation = statusValidation;
        this.setStatus = setStatus;
        this.fileAuthorisation = fileAuthorisation;
        this.fileValidation = fileValidation;
        this.fileToDirectory = fileToDirectory;
        this.datesAuthorisation = datesAuthorisation;
        this.datesValidation = datesValidation;
        this.recordDates = recordDates;
    }

    [Function(nameof(SetFormSubmissionStatus))]
    public async Task<IActionResult> SetStatus(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "form-submissions/{formSubmissionId}/status")] HttpRequest request,
        string formSubmissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SetFormSubmissionStatus>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say which status.");
        var command = posted with { FormSubmissionId = formSubmissionId, HandledByEmail = signedInUser.Email };
        var mayAct = statusAuthorisation.Allows(signedInUser, command)
            && await FormRecordScope.MayActOnFormAsync(context, signedInUser, formSubmissionId, cancellationToken);
        if (!mayAct) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(setStatus, command, statusValidation.Check(command), cancellationToken);
    }

    [Function(nameof(FileFormToDirectory))]
    public async Task<IActionResult> FileToDirectory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-submissions/{formSubmissionId}/file-to-directory")] HttpRequest request,
        string formSubmissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<FileFormToDirectory>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say which files go to which company.");
        var command = posted with { FormSubmissionId = formSubmissionId, FiledByEmail = signedInUser.Email };
        var mayAct = fileAuthorisation.Allows(signedInUser, command)
            && await FormRecordScope.MayActOnFormAsync(context, signedInUser, formSubmissionId, cancellationToken);
        if (!mayAct) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(fileToDirectory, command, fileValidation.Check(command), cancellationToken);
    }

    [Function(nameof(RecordFormFolderDates))]
    public async Task<IActionResult> RecordDates(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "form-folders/{formFolderId}/dates")] HttpRequest request,
        string formFolderId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<RecordFormFolderDates>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say which dates.");
        var command = posted with { FormFolderId = formFolderId, RecordedByEmail = signedInUser.Email };
        var mayAct = datesAuthorisation.Allows(signedInUser, command)
            && await FormRecordScope.MayDateFolderAsync(context, signedInUser, formFolderId, cancellationToken);
        if (!mayAct) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(recordDates, command, datesValidation.Check(command), cancellationToken);
    }
}
