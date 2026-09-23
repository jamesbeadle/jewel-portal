namespace Jewel.JPMS.Features.Forms.Office;

/// <summary>
/// One form as the office reads it: its answers in the form's own order, the files under the
/// question that asked for them, and the answers held back until someone reveals them — which,
/// once revealed, read in place like any other answer.
/// </summary>
public sealed class FormSubmissionReading
{
    private readonly IReadOnlyDictionary<string, string> revealed;

    public FormSubmissionReading(FormSubmissionView view, IReadOnlyDictionary<string, string>? revealed)
    {
        View = view;
        this.revealed = revealed ?? new Dictionary<string, string>();
    }

    public FormSubmissionView View { get; }

    public FormSubmission Submission => View.Submission;

    public FormDefinition? Form => FormCatalogue.For(View.Submission.FormSlug);

    public bool HasHiddenAnswers => View.WithheldKeys.Any(key => !revealed.ContainsKey(key));

    public bool IsWithheld(string key) => View.WithheldKeys.Contains(key) && !revealed.ContainsKey(key);

    public string AnswerTo(string key) => revealed.TryGetValue(key, out var shown) ? shown : View.Answers.GetValueOrDefault(key, "");

    public IReadOnlyList<FormUploadedFile> FilesFor(string key) => View.Files.Where(file => file.QuestionKey == key).ToList();

    public IReadOnlyList<FormUploadedFile> OtherFiles => View.Files.Where(file => Form?.QuestionFor(file.QuestionKey) is null).ToList();

    public IReadOnlyList<FormUploadedFile> FilesStillHeld => View.Files.Where(file => file.DeletedAt is null).ToList();

    public bool IsWorthShowing(FormQuestion question) =>
        question.IsShownFor(View.Answers) || AnswerTo(question.Key).Length > 0 || FilesFor(question.Key).Count > 0;
}
