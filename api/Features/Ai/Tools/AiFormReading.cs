namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// One form as the connector reads it: the questions in the form's order with their answers — a
/// health answer hidden until revealed on the page, a sensitive one kept there — the files with their
/// ids, and what the office does next with this kind of form, ready for the action that does it.
/// </summary>
internal static class AiFormReading
{
    private const string OnThePage = "withheld from the connector — read it on the form's page";
    private const string HiddenHealthAnswer = "a health answer, hidden until revealed on the form's page";
    private const string DrawnSignature = " (drawn signature on file)";

    public static object Of(FormSubmissionView view, DateTimeOffset now)
    {
        var slug = view.Submission.FormSlug;
        var form = FormCatalogue.For(slug);
        var isFiledToTheDirectory = slug is FormSlugs.SubcontractorQuestionnaire or FormSlugs.InsuranceUpdate;
        var isATrainingCertificate = slug == FormSlugs.TrainingCertificate;
        var isAVehicleForm = slug == FormSlugs.CompanyVehicle;
        return new
        {
            ok = true,
            submission = SubmissionRow(view.Submission),
            quizScore = view.QuizScore is { } score ? new { score.Score, score.OutOf, score.PassMark, score.HasPassed } : null,
            answers = (form?.AskedQuestions ?? Array.Empty<FormQuestion>()).Select(question => AnswerRow(view, question)),
            files = view.Files.Select(FileRow),
            filingSuggestions = isFiledToTheDirectory ? FormDirectoryFilingPlan.For(view) : null,
            trainingSuggestion = isATrainingCertificate ? TrainingCertificateForm.DetailsFrom(view.Submission, view.Answers) : null,
            checkCodeDaysLeft = isAVehicleForm ? DrivingLicenceChecks.CheckCodeDaysLeft(view.Submission.SubmittedAt, now) : (int?)null
        };
    }

    public static object SubmissionRow(FormSubmission submission) => new
    {
        submission.FormSubmissionId, submission.FormSlug, title = FormCatalogue.TitleOf(submission.FormSlug),
        submission.SubmitterName, submission.FilingName, submission.FormFolderId,
        submission.IsVerifiedLink, submission.SentToEmail, submission.SentByName, submission.FormPackId,
        status = submission.Status.ToString(), submission.SubmittedAt, submission.HandledByEmail, submission.HandledAt
    };

    private static object AnswerRow(FormSubmissionView view, FormQuestion question) =>
        new { question.Key, question.Label, kind = question.Kind.ToString(), answer = AnswerTo(view, question) };

    private static string AnswerTo(FormSubmissionView view, FormQuestion question) => question switch
    {
        _ when view.WithheldKeys.Contains(question.Key) => HiddenHealthAnswer,
        _ when SensitiveAnswers.IsSensitive(question) => OnThePage,
        { Kind: FormQuestionKind.Signature } => view.Answers.GetValueOrDefault(FormAnswerRules.SignatureNameKey(question.Key), "") + DrawnSignature,
        { Kind: FormQuestionKind.Upload } => $"{view.Files.Count(file => file.QuestionKey == question.Key)} file(s)",
        _ => view.Answers.GetValueOrDefault(question.Key, "")
    };

    private static object FileRow(FormUploadedFile file) => new
    {
        file.FormUploadId, file.QuestionKey, file.FileName, file.ContentType, file.Size,
        isDeleted = file.DeletedAt is not null, file.DeletionReason
    };
}
