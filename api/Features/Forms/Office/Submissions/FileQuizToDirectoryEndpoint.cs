using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>The office files a marked quiz to the directory company it was taken for.</summary>
public sealed class FileQuizToDirectoryEndpoint
{
    private readonly JpmsContext context;
    private readonly SignedInUserResolver users;
    private readonly FileQuizToDirectoryAuthorisation authorisation;
    private readonly FileQuizToDirectoryValidation validation;
    private readonly ICommandHandler<FileQuizToDirectory, FormDirectoryFiling> fileQuiz;

    public FileQuizToDirectoryEndpoint(
        JpmsContext context, SignedInUserResolver users,
        FileQuizToDirectoryAuthorisation authorisation, FileQuizToDirectoryValidation validation,
        ICommandHandler<FileQuizToDirectory, FormDirectoryFiling> fileQuiz)
    {
        this.context = context;
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.fileQuiz = fileQuiz;
    }

    [Function(nameof(FileQuizToDirectory))]
    public async Task<IActionResult> FileQuiz(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-submissions/{formSubmissionId}/file-quiz-to-directory")] HttpRequest request,
        string formSubmissionId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<FileQuizToDirectory>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say which company the quiz goes to.");
        var command = posted with { FormSubmissionId = formSubmissionId, FiledByEmail = signedInUser.Email };
        var mayAct = authorisation.Allows(signedInUser, command)
            && await FormRecordScope.MayActOnFormAsync(context, signedInUser, formSubmissionId, cancellationToken);
        if (!mayAct) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(fileQuiz, command, validation.Check(command), cancellationToken);
    }
}
