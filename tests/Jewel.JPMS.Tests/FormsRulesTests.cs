using Jewel.JPMS.Api.Features.Forms.Answers;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The forms moved from Jeremy's forms dashboard (2026-09-23): the rules a form, a pack, a right-to-work
// check and the retention clocks are held to, as the dashboard held them.
public sealed class FormsRulesTests
{
    private static readonly FormDefinition Vehicle = CompanyVehicleForm.Definition;

    [Theory]
    [InlineData("ni_number", true)]
    [InlineData("utr", true)]
    [InlineData("dob", true)]
    [InlineData("share_code", true)]
    [InlineData("unspent", true)]
    [InlineData("points_detail", true)]
    [InlineData("name", false)]
    [InlineData("email", false)]
    [InlineData("company", false)]
    public void SensitiveKeys_areTheDashboardsList(string key, bool isSensitive) =>
        Assert.Equal(isSensitive, SensitiveAnswers.IsSensitiveKey(key));

    [Fact]
    public void HealthAnswers_areSensitiveWhateverTheirKey()
    {
        var support = EmergencyContactForm.Definition.QuestionFor("support_detail")!;
        Assert.False(SensitiveAnswers.IsSensitiveKey(support.Key));
        Assert.True(SensitiveAnswers.IsSensitive(support));
    }

    [Fact]
    public void FirstProblem_isTheFirstQuestionAskedInOrder()
    {
        var problem = FormAnswerRules.FirstProblem(Vehicle, new Dictionary<string, string>(), new Dictionary<string, int>());
        Assert.Equal(FormWording.PleaseFillIn("Full name (as shown on your licence)"), problem);
    }

    [Fact]
    public void AConditionalQuestion_isOnlyAskedWhileItsAnswerHolds()
    {
        var training = TrainingCertificateForm.Definition;
        var answers = new Dictionary<string, string> { ["name"] = "Sam", ["course"] = "CSCS", ["completed"] = "2026-09-01" };
        var problem = FormAnswerRules.FirstProblem(training, answers, new Dictionary<string, int>());
        Assert.Equal(FormWording.NeedsAFile("Photo or PDF of the certificate"), problem);
        var sent = FormAnswerRules.FirstProblem(training, answers, new Dictionary<string, int> { ["certificate"] = 1 });
        Assert.Null(sent);
        var detail = Vehicle.QuestionFor("points_detail")!;
        Assert.False(detail.IsShownFor(new Dictionary<string, string> { ["points"] = FormWording.No }));
        Assert.True(detail.IsShownFor(new Dictionary<string, string> { ["points"] = FormWording.Yes }));
    }

    [Fact]
    public void ASignature_needsTheTypedNameAndADrawing()
    {
        var form = RightToWorkForm.Definition;
        var answers = new Dictionary<string, string>
        {
            ["full_name"] = "Sam Smith", ["dob"] = "1990-01-01", ["email"] = "sam@example.com", ["mobile"] = "07700 900000",
            ["status"] = RightToWorkForm.BritishOrIrishCitizen, ["engaged_as"] = RightToWorkForm.Employee,
            ["declaration_name"] = "Sam Smith"
        };
        var unsigned = FormAnswerRules.FirstProblem(form, answers, new Dictionary<string, int>());
        Assert.Equal(FormWording.PleaseSign("Declaration"), unsigned);
        Assert.Null(FormAnswerRules.FirstProblem(form, answers, new Dictionary<string, int> { ["declaration"] = 1 }));
    }

    [Fact]
    public void ADeclaration_isKeptOnlyAsTicked_andAnAnswerToAQuestionNeverShownIsNotKept()
    {
        var posted = new Dictionary<string, string>
        {
            ["dec_accurate"] = "true", ["dec_policy"] = FormWording.Yes, ["points"] = FormWording.No, ["points_detail"] = "SP30 in 2025"
        };

        var kept = FormAnswerCleaning.Kept(Vehicle, posted);

        Assert.False(kept.ContainsKey("dec_accurate"));
        Assert.Equal(FormWording.Yes, kept["dec_policy"]);
        Assert.False(kept.ContainsKey("points_detail"));
    }

    [Fact]
    public void APack_holdsTheFormsTheAnswersSay()
    {
        var employee = FormPackPlanner.FormsFor(Engagement.Employee, new FormPackAnswers(false, true, false, true));
        Assert.Equal(new[] { FormSlugs.NewStarter, FormSlugs.EmergencyContact, FormSlugs.RightToWork,
            FormSlugs.WorkstationAssessment, FormSlugs.TrainingCertificate }, employee);
        var subcontractor = FormPackPlanner.FormsFor(Engagement.SelfEmployed, new FormPackAnswers(false, false, true, false));
        Assert.Equal(new[] { FormSlugs.EmergencyContact, FormSlugs.RightToWork, FormSlugs.CompanyVehicle }, subcontractor);
    }

    [Fact]
    public void APack_isKeptAliveByUse_butNeverPastSixtyDays()
    {
        var sent = new DateTimeOffset(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);
        var expires = sent + FormLinkLifetimes.PackOnSending;
        Assert.Equal(expires, FormLinkLifetimes.PackKeptAliveAt(expires, sent, sent.AddDays(2)));
        Assert.Equal(sent.AddDays(20), FormLinkLifetimes.PackKeptAliveAt(expires, sent, sent.AddDays(13)));
        Assert.Equal(sent.AddDays(FormLinkLifetimes.LongestDays), FormLinkLifetimes.PackKeptAliveAt(expires, sent, sent.AddDays(58)));
    }

    [Fact]
    public void TheQuestionnaire_isJewelBespokeBuilds_amongTheDashboardsNineForms_andKatyLouisesEight()
    {
        var questionnaire = FormCatalogue.For(FormSlugs.SubcontractorQuestionnaire)!;
        Assert.Equal("Sub Contractor Questionnaire 2025", questionnaire.Title);
        Assert.Equal(17, FormCatalogue.All.Count);
        Assert.Equal(8, FormCatalogue.HealthAndSafety.Count);
        Assert.All(FormCatalogue.HealthAndSafety, form => Assert.Equal(FormFilingKind.Site, form.FilingKind));
        Assert.All(FormCatalogue.HealthAndSafety, form => Assert.Contains("site", form.FilingKeys));
    }
}
