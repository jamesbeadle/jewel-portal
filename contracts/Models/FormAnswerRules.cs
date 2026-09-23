namespace Jewel.JPMS.Models;

/// <summary>
/// What a form needs before it can be sent, checked in the order the form asks it — the dashboard's
/// submit loop, run by the page before it posts and again by the API before it saves, so an old page
/// on a phone cannot send what the form would refuse. One problem at a time, as the page shows it.
/// A signature is the typed name under its own key plus a drawing uploaded under the question's key.
/// </summary>
public static class FormAnswerRules
{
    public const string SignatureNameSuffix = "_name";

    public static string SignatureNameKey(string questionKey) => questionKey + SignatureNameSuffix;

    public static string? FirstProblem(
        FormDefinition form, IReadOnlyDictionary<string, string> answers, IReadOnlyDictionary<string, int> fileCounts) =>
        form.AskedQuestions
            .Where(question => question.IsRequired && question.IsShownFor(answers))
            .Select(question => ProblemWith(question, answers, fileCounts))
            .FirstOrDefault(problem => problem is not null);

    private static string? ProblemWith(
        FormQuestion question, IReadOnlyDictionary<string, string> answers, IReadOnlyDictionary<string, int> fileCounts)
    {
        var answer = answers.GetValueOrDefault(question.Key, "").Trim();
        var files = fileCounts.GetValueOrDefault(question.Key);
        return question.Kind switch
        {
            FormQuestionKind.Upload => files > 0 ? null : FormWording.NeedsAFile(question.Label),
            FormQuestionKind.Declaration => answer == FormWording.Yes ? null : FormWording.PleaseTick(question.Label),
            FormQuestionKind.Signature => IsSigned(question, answers, files) ? null : FormWording.PleaseSign(question.Label),
            FormQuestionKind.Table => HasARow(question, answer) ? null : FormWording.PleaseFillIn(question.Label),
            _ => answer.Length > 0 ? null : FormWording.PleaseFillIn(question.Label)
        };
    }

    private static bool HasARow(FormQuestion question, string answer) =>
        question.Table is { } table && FormTableAnswers.HasAnAnswer(table, answer);

    private static bool IsSigned(FormQuestion question, IReadOnlyDictionary<string, string> answers, int drawings)
    {
        var typedName = answers.GetValueOrDefault(SignatureNameKey(question.Key), "").Trim();
        return typedName.Length > 0 && drawings > 0;
    }
}
