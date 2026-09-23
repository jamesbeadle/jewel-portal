using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Answers;

/// <summary>
/// What a public form is allowed to store (api/forms-intake.js, tightened): only the keys the form
/// asks — a signature's typed name under its own key — each trimmed and capped, a declaration only
/// as ticked or not, a register's rows only as its own columns, and nothing for a question the
/// person was never shown.
/// </summary>
internal static class FormAnswerCleaning
{
    public static Dictionary<string, string> Kept(FormDefinition form, IReadOnlyDictionary<string, string> posted)
    {
        var kept = new Dictionary<string, string>();
        foreach (var question in form.AskedQuestions.Where(question => question.Kind != FormQuestionKind.Upload))
        {
            var key = question.Kind == FormQuestionKind.Signature ? FormAnswerRules.SignatureNameKey(question.Key) : question.Key;
            var answer = Capped(posted.GetValueOrDefault(key, ""), question);
            if (answer.Length > 0) kept[key] = answer;
        }
        return WithoutHiddenAnswers(form, kept);
    }

    private static string Capped(string answer, FormQuestion question)
    {
        var table = question.Table;
        if (table is not null) return CleanedRows(table, answer ?? "");
        var trimmed = (answer ?? "").Trim();
        var longest = question.Kind == FormQuestionKind.LongText ? PublicFormLimits.LongestTextBox : PublicFormLimits.LongestAnswer;
        var isTicked = trimmed == FormWording.Yes;
        if (question.Kind == FormQuestionKind.Declaration) return isTicked ? FormWording.Yes : "";
        return trimmed.Length > longest ? trimmed[..longest] : trimmed;
    }

    private static string CleanedRows(FormTable table, string answer)
    {
        var rows = FormTableAnswers.Cleaned(table, answer);
        return FormTableAnswers.Write(rows);
    }

    private static Dictionary<string, string> WithoutHiddenAnswers(FormDefinition form, Dictionary<string, string> answers)
    {
        var hiddenKeys = form.AskedQuestions.Where(question => !question.IsShownFor(answers)).Select(question => question.Key);
        foreach (var key in hiddenKeys.ToList()) answers.Remove(key);
        return answers;
    }
}
