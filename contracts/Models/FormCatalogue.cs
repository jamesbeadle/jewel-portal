namespace Jewel.JPMS.Models;

/// <summary>The forms' addresses, kept exactly as the dashboard's /forms/&lt;slug&gt; so a redirect maps one to one.</summary>
public static class FormSlugs
{
    public const string SubcontractorQuestionnaire = "subcontractor";
    public const string JewelBespokeBuildQuestionnaire = "subcontractor-jbb";
    public const string AccidentReport = "accident";
    public const string CompanyVehicle = "vehicle";
    public const string EmergencyContact = "emergency";
    public const string InsuranceUpdate = "insurance";
    public const string TrainingCertificate = "training";
    public const string RightToWork = "rtw";
    public const string NewStarter = "starter";
    public const string WorkstationAssessment = "dse";
}

/// <summary>
/// Every form the portal serves. The JBB questionnaire is no longer a second form: its old
/// address answers with the one questionnaire, which takes the JBB title when it is sent for
/// Jewel Bespoke Build.
/// </summary>
public static class FormCatalogue
{
    public static IReadOnlyList<FormDefinition> All { get; } = new[]
    {
        NewStarterChecklistForm.Definition,
        EmergencyContactForm.Definition,
        RightToWorkForm.Definition,
        WorkstationAssessmentForm.Definition,
        CompanyVehicleForm.Definition,
        TrainingCertificateForm.Definition,
        VehicleAccidentReportForm.Definition,
        SubcontractorQuestionnaireForm.Definition,
        InsuranceUpdateForm.Definition
    };

    public static FormDefinition? For(string? slug)
    {
        var current = slug == FormSlugs.JewelBespokeBuildQuestionnaire ? FormSlugs.SubcontractorQuestionnaire : slug;
        return All.FirstOrDefault(definition => definition.Slug == current);
    }

    public static string TitleOf(string slug) => For(slug)?.Title ?? slug;

    public static string TitleOf(string slug, JewelCompany company) => For(slug)?.TitleFor(company) ?? slug;
}
