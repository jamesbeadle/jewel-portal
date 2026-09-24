namespace Jewel.JPMS.Api.Features.Forms.Quizzes;

/// <summary>
/// A quiz's answer key: for each scored question, the place of its right answer among the choices
/// the form offers, so the words are written once, in the definition. Held here, in the api, because
/// the definition travels to the browser and the key must not.
/// </summary>
public sealed record FormQuiz(string FormSlug, int PassMark, IReadOnlyDictionary<string, int> RightChoiceByQuestion);

/// <summary>Every form the portal marks, and the one rule that marks it: a point for each right answer.</summary>
public static class FormQuizzes
{
    public static readonly FormQuiz CyberQuiz = new(FormSlugs.CyberQuiz, PassMark: 23, RightChoiceByQuestion: new Dictionary<string, int>
    {
        ["installing_software"] = 2, ["passwords_and_mfa"] = 1, ["suspicious_request"] = 2, ["fake_payment_link"] = 3,
        ["acceptable_use"] = 2, ["company_card"] = 1, ["personal_card"] = 1, ["card_problem"] = 2,
        ["approved_ai_tools"] = 1, ["software_updates"] = 1, ["data_in_ai"] = 1, ["ai_outputs"] = 1,
        ["ai_best_practice"] = 0, ["monitoring"] = 0, ["individual_monitoring"] = 1, ["shadow_it"] = 1,
        ["storing_documents"] = 1, ["lost_device"] = 1, ["copilot_contracts"] = 1, ["reporting_concerns"] = 2,
        ["screen_lock"] = 1, ["public_wifi"] = 1, ["data_breach"] = 1, ["deepfakes"] = 1, ["usb_sticks"] = 1
    });

    public static IReadOnlyList<FormQuiz> All { get; } = new[] { CyberQuiz };

    public static FormQuizScore? Mark(string formSlug, IReadOnlyDictionary<string, string> answers)
    {
        var quiz = All.FirstOrDefault(candidate => candidate.FormSlug == formSlug);
        var form = FormCatalogue.For(formSlug);
        if (quiz is null || form is null) return null;
        var score = quiz.RightChoiceByQuestion.Count(pair => IsRight(form, pair.Key, pair.Value, answers));
        return new FormQuizScore(score, quiz.RightChoiceByQuestion.Count, quiz.PassMark);
    }

    public static string RightAnswerTo(FormDefinition form, string questionKey, int rightChoice) =>
        form.QuestionFor(questionKey)?.Choices.ElementAtOrDefault(rightChoice) ?? "";

    private static bool IsRight(FormDefinition form, string questionKey, int rightChoice, IReadOnlyDictionary<string, string> answers)
    {
        var given = answers.GetValueOrDefault(questionKey, "").Trim();
        var rightAnswer = RightAnswerTo(form, questionKey, rightChoice);
        return rightAnswer.Length > 0 && given == rightAnswer;
    }
}
