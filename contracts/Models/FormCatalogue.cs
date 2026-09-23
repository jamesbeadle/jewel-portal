namespace Jewel.JPMS.Models;

/// <summary>The forms' addresses, kept exactly as the dashboard's /forms/&lt;slug&gt; so a redirect maps one to one.</summary>
public static class FormSlugs
{
    public const string SubcontractorQuestionnaire = "subcontractor";
    public const string AccidentReport = "accident";
    public const string CompanyVehicle = "vehicle";
    public const string EmergencyContact = "emergency";
    public const string InsuranceUpdate = "insurance";
    public const string TrainingCertificate = "training";
    public const string RightToWork = "rtw";
    public const string NewStarter = "starter";
    public const string WorkstationAssessment = "dse";
}

/// <summary>Every form the portal serves.</summary>
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

    public static FormDefinition? For(string? slug) => All.FirstOrDefault(definition => definition.Slug == slug);

    public static string TitleOf(string slug) => For(slug)?.Title ?? slug;
}
