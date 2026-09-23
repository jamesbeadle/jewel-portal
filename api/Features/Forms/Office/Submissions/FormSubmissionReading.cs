using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Api.Features.Forms.Mapping;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// A submission as the office reads it: every answer in the form's order, the files, and the health
/// answers held back until someone reveals them. A form kept in a restricted store is read only by
/// that store's readers; everyone in the office may see that it came in, and a site role sees only
/// the health and safety forms — its own — on the Received list.
/// </summary>
internal static class FormSubmissionReading
{
    public static bool MayRead(SignedInUser user, FormSubmissionEntity submission) => MayRead(user, submission.FormSlug);

    public static bool MayRead(SignedInUser user, string formSlug)
    {
        var form = FormCatalogue.For(formSlug);
        var store = form?.Store ?? FormEvidenceStore.General;
        return FormRoleSets.ReadersOf(store).IncludesAny(user.Roles);
    }

    public static IReadOnlyList<FormSubmission> VisibleTo(SignedInUser user, IReadOnlyList<FormSubmission> submissions) =>
        submissions.Where(submission => MayRead(user, submission.FormSlug)).ToList();

    public static FormSubmissionView ViewOf(FormSubmissionEntity submission, IEnumerable<FormUploadEntity> uploads)
    {
        var form = FormCatalogue.For(submission.FormSlug);
        var answers = FormAnswersJson.Read(submission.AnswersJson);
        var withheld = HealthKeys(form).Where(answers.ContainsKey).ToList();
        foreach (var key in withheld) answers.Remove(key);
        var files = uploads.OrderBy(upload => upload.UploadedAt).Select(upload => upload.ToModel()).ToList();
        return new FormSubmissionView(submission.ToModel(), answers, files, withheld);
    }

    public static IReadOnlyDictionary<string, string> HealthAnswersOf(FormSubmissionEntity submission)
    {
        var form = FormCatalogue.For(submission.FormSlug);
        var answers = FormAnswersJson.Read(submission.AnswersJson);
        return HealthKeys(form)
            .Where(key => answers.GetValueOrDefault(key, "").Trim().Length > 0)
            .ToDictionary(key => key, key => answers[key]);
    }

    private static IEnumerable<string> HealthKeys(FormDefinition? form) =>
        form?.Questions.Where(question => question.IsSpecialCategory).Select(question => question.Key) ?? Enumerable.Empty<string>();
}
