namespace Jewel.JPMS.Models;

/// <summary>The forms' addresses, kept exactly as the dashboard's /forms/&lt;slug&gt; so a redirect maps one to one;
/// the health and safety forms (2026-09-23, Katy-Louise's paper sheets) are named for the sheet each one is.</summary>
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
    public const string ToolboxTalk = "toolbox-talk";
    public const string LadderInspection = "ladder-inspection";
    public const string EquipmentSchedule = "equipment-schedule";
    public const string PuwerInspection = "puwer-inspection";
    public const string SiteIncident = "site-incident";
    public const string PersonnelIncident = "personnel-incident";
    public const string FirstAidKit = "first-aid-kit";
    public const string FireExtinguishers = "fire-extinguishers";
    public const string CyberQuiz = "cyber-quiz";
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
        InsuranceUpdateForm.Definition,
        CyberQuizForm.Definition,
        ToolboxTalkRegisterForm.Definition,
        LadderInspectionRecordForm.Definition,
        WorkEquipmentScheduleForm.Definition,
        PuwerInspectionRecordForm.Definition,
        SiteIncidentReportForm.Definition,
        PersonnelIncidentReportForm.Definition,
        FirstAidKitChecklistForm.Definition,
        FireExtinguisherInspectionForm.Definition
    };

    /// <summary>The H&amp;S officer's site checks and the site's incident reports — the forms in the health and safety store, in the order she runs them.</summary>
    public static IReadOnlyList<FormDefinition> HealthAndSafety { get; } =
        All.Where(definition => definition.Store == FormEvidenceStore.HealthAndSafety).ToList();

    public static FormDefinition? For(string? slug) => All.FirstOrDefault(definition => definition.Slug == slug);

    public static string TitleOf(string slug) => For(slug)?.Title ?? slug;
}
