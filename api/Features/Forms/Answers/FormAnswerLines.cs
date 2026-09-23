namespace Jewel.JPMS.Api.Features.Forms.Answers;

/// <summary>One answer as a person reads it: the question's words and what was given.</summary>
public sealed record FormAnswerLine(string QuestionKey, string Label, string Answer);

/// <summary>
/// A form's answers in the order the form asks them, with the question's own words rather than the
/// raw key. Echoed lines — the person's copy and the office alert — leave out every sensitive
/// answer; the office's own reading keeps them all.
/// </summary>
public static class FormAnswerLines
{
    public static IReadOnlyList<FormAnswerLine> AllOf(FormDefinition form, IReadOnlyDictionary<string, string> answers) =>
        form.AskedQuestions.Select(question => LineFor(question, answers)).OfType<FormAnswerLine>().ToList();

    public static IReadOnlyList<FormAnswerLine> SafeToEcho(FormDefinition form, IReadOnlyDictionary<string, string> answers) =>
        form.AskedQuestions.Where(question => !SensitiveAnswers.IsSensitive(question))
            .Select(question => LineFor(question, answers)).OfType<FormAnswerLine>().ToList();

    private static FormAnswerLine? LineFor(FormQuestion question, IReadOnlyDictionary<string, string> answers)
    {
        var isSignature = question.Kind == FormQuestionKind.Signature;
        var key = isSignature ? FormAnswerRules.SignatureNameKey(question.Key) : question.Key;
        var answer = answers.GetValueOrDefault(key, "").Trim();
        if (answer.Length == 0) return null;
        return new FormAnswerLine(question.Key, question.Label, isSignature ? "Signed by " + answer : answer);
    }
}
