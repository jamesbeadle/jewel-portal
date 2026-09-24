using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms.Quizzes;

namespace Jewel.JPMS.Api.Features.Forms.Public;

public sealed partial class PublicFormService
{
    private const string QuizScoreKey = "quiz_score";

    /// <summary>
    /// The office is alerted straight away — an accident report to the people who must hear of it
    /// that hour — and a person who came through a one-time link gets their own copy. Neither email
    /// can fail the form: it is already saved, and a missed email is logged, not thrown.
    /// </summary>
    private async Task AnnounceAsync(
        FormDefinition form, FormSubmissionEntity submission, IReadOnlyDictionary<string, string> answers,
        IReadOnlyList<FormUploadEntity> uploads)
    {
        var echoed = WithQuizScore(form, FormAnswerLines.SafeToEcho(form, answers), answers);
        var fileNames = uploads.Where(upload => IsSafeToName(form, upload)).Select(upload => upload.FileName).ToList();
        var alert = AlertFor(form, submission, echoed, fileNames);
        await SendQuietlyAsync(alert);
        if (!submission.IsVerifiedLink) return;
        var copy = FormReceiptEmails.CopyForThePerson(form, submission.SubmitterName, submission.SentToEmail, echoed, fileNames);
        await SendQuietlyAsync(copy);
    }

    private FormEmail AlertFor(
        FormDefinition form, FormSubmissionEntity submission, IReadOnlyList<FormAnswerLine> echoed,
        IReadOnlyList<string> fileNames)
    {
        var isRestricted = form.Store != FormEvidenceStore.General;
        var lines = isRestricted ? Array.Empty<FormAnswerLine>() : echoed;
        var files = isRestricted ? Array.Empty<string>() : fileNames;
        var to = form.IsAnAccidentReport ? options.AccidentAlert : options.OfficeAlert;
        var officeLink = options.OfficeLink(submission.FormSubmissionId);
        return FormReceiptEmails.AlertForTheOffice(form, submission.SubmitterName, to, lines, files, officeLink);
    }

    private static IReadOnlyList<FormAnswerLine> WithQuizScore(
        FormDefinition form, IReadOnlyList<FormAnswerLine> echoed, IReadOnlyDictionary<string, string> answers)
    {
        var score = FormQuizzes.Mark(form.Slug, answers);
        if (score is null) return echoed;
        return echoed.Append(new FormAnswerLine(QuizScoreKey, "Score", score.Sentence)).ToList();
    }

    private static bool IsSafeToName(FormDefinition form, FormUploadEntity upload)
    {
        var question = form.QuestionFor(upload.QuestionKey);
        return question is not null && !SensitiveAnswers.IsSensitive(question);
    }

    private async Task SendQuietlyAsync(FormEmail email)
    {
        if (!mailer.IsConfigured || email.To.Count == 0) return;
        try { await mailer.SendAsync(email, CancellationToken.None); }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            logger.LogWarning(failure, "Forms: an email about a sent form was not delivered.");
        }
    }
}
