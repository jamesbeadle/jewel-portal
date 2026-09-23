namespace Jewel.JPMS.Models;

/// <summary>
/// Which forms a new starter owes, decided from how they are engaged and four answers rather than
/// from the office's memory: right to work and an emergency contact from everybody, the HMRC
/// checklist from an employee with no P45, the workstation assessment from anyone at a screen, the
/// vehicle form where a vehicle is being allocated, a training certificate where the role needs a ticket.
/// </summary>
public static class FormPackPlanner
{
    public static IReadOnlyList<string> FormsFor(Engagement engagedAs, FormPackAnswers answers)
    {
        var forms = new List<string>();
        var needsTheStarterChecklist = engagedAs == Engagement.Employee && !answers.HasP45;
        if (needsTheStarterChecklist) forms.Add(FormSlugs.NewStarter);
        forms.Add(FormSlugs.EmergencyContact);
        forms.Add(FormSlugs.RightToWork);
        if (answers.IsWorkingAtAScreen) forms.Add(FormSlugs.WorkstationAssessment);
        if (answers.IsGettingAVehicle) forms.Add(FormSlugs.CompanyVehicle);
        if (answers.MustHoldATicket) forms.Add(FormSlugs.TrainingCertificate);
        return forms;
    }
}
