namespace Jewel.JPMS.Models;

/// <summary>
/// A marked quiz as a document on a company's compliance record. One kind, so a retake supersedes
/// the last result rather than sitting beside it; the result is in the file's name. A pass stands for
/// a year, as the quiz is taken yearly, and the register then asks for it again; a fail expires the
/// day it was sent, so the company's standing reads Expired until a retake passes.
/// </summary>
public static class QuizComplianceRecord
{
    public const string Kind = "IT, Cyber & AI quiz";
    private const int PassStandsForYears = 1;

    public static string FileNameFor(FormQuizScore score) => $"{Kind} - {score.Score} of {score.OutOf}, {score.Outcome}.pdf";

    public static DateOnly ExpiresOn(FormQuizScore score, DateTimeOffset submittedAt)
    {
        var sentOn = DateOnly.FromDateTime(submittedAt.UtcDateTime);
        return score.HasPassed ? sentOn.AddYears(PassStandsForYears) : sentOn;
    }
}
