using Jewel.JPMS.Api.Features.Forms.Quizzes;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The IT, Cyber, AI & Monitoring Quiz (2026-09-24, from JPS's Microsoft Form, ported for Jewel Bespoke
// Build): 25 questions of one point, a pass at 23, the key held in the api and read against the
// definition's own choices, and a result that files to the directory as the company's current quiz.
public sealed class CyberQuizTests
{
    private static readonly FormDefinition Quiz = CyberQuizForm.Definition;

    private static Dictionary<string, string> AnsweredRightExcept(int wrongCount)
    {
        var answers = new Dictionary<string, string>();
        var wrongLeft = wrongCount;
        foreach (var (key, rightChoice) in FormQuizzes.CyberQuiz.RightChoiceByQuestion)
        {
            var choices = Quiz.QuestionFor(key)!.Choices;
            var isToBeWrong = wrongLeft-- > 0;
            answers[key] = choices[isToBeWrong ? (rightChoice + 1) % choices.Count : rightChoice];
        }
        return answers;
    }

    [Fact]
    public void TheKey_marksEveryChoiceQuestion_onceEach_withAChoiceTheFormOffers()
    {
        var choiceKeys = Quiz.AskedQuestions.Where(question => question.Kind == FormQuestionKind.Choice).Select(question => question.Key);
        Assert.Equal(25, FormQuizzes.CyberQuiz.RightChoiceByQuestion.Count);
        Assert.Equal(choiceKeys.OrderBy(key => key), FormQuizzes.CyberQuiz.RightChoiceByQuestion.Keys.OrderBy(key => key));
        Assert.All(FormQuizzes.CyberQuiz.RightChoiceByQuestion,
            pair => Assert.NotEmpty(FormQuizzes.RightAnswerTo(Quiz, pair.Key, pair.Value)));
    }

    [Theory]
    [InlineData(0, 25, true)]
    [InlineData(2, 23, true)]
    [InlineData(3, 22, false)]
    [InlineData(25, 0, false)]
    public void AQuiz_scoresAPointPerRightAnswer_andPassesAt23(int wrongCount, int expectedScore, bool expectedToPass)
    {
        var score = FormQuizzes.Mark(FormSlugs.CyberQuiz, AnsweredRightExcept(wrongCount))!;

        Assert.Equal(expectedScore, score.Score);
        Assert.Equal(25, score.OutOf);
        Assert.Equal(expectedToPass, score.HasPassed);
    }

    [Fact]
    public void AnUnansweredQuiz_scoresNothing_andAnyOtherFormIsNotMarked()
    {
        Assert.Equal(0, FormQuizzes.Mark(FormSlugs.CyberQuiz, new Dictionary<string, string>())!.Score);
        Assert.Null(FormQuizzes.Mark(FormSlugs.SubcontractorQuestionnaire, new Dictionary<string, string>()));
    }

    [Fact]
    public void TheQuiz_isJewelBespokeBuilds_notJps_andIsFiledUnderTheCompany()
    {
        var words = Quiz.Questions.SelectMany(question => question.Choices.Append(question.Label)).Append(Quiz.Intro).Append(Quiz.Title);

        Assert.DoesNotContain(words, text => text.Contains("JPS", StringComparison.Ordinal) || text.Contains("tenant or staff", StringComparison.Ordinal));
        Assert.Equal(FormFilingKind.Company, Quiz.FilingKind);
        Assert.Equal(new[] { "company" }, Quiz.FilingKeys);
        Assert.Same(Quiz, FormCatalogue.For(FormSlugs.CyberQuiz));
    }

    [Fact]
    public void APass_standsForAYear_andAFail_expiresTheDayItWasSent()
    {
        var sentAt = new DateTimeOffset(2026, 9, 24, 10, 0, 0, TimeSpan.Zero);
        var passed = new FormQuizScore(24, 25, 23);
        var failed = new FormQuizScore(20, 25, 23);

        Assert.Equal(new DateOnly(2027, 9, 24), QuizComplianceRecord.ExpiresOn(passed, sentAt));
        Assert.Equal(new DateOnly(2026, 9, 24), QuizComplianceRecord.ExpiresOn(failed, sentAt));
        Assert.Equal("IT, Cyber & AI quiz - 24 of 25, passed.pdf", QuizComplianceRecord.FileNameFor(passed));
        Assert.Equal("IT, Cyber & AI quiz - 20 of 25, failed.pdf", QuizComplianceRecord.FileNameFor(failed));
    }
}
