namespace Jewel.JPMS.Models;

/// <summary>
/// How long form evidence is kept (lib/retention.js — Jeremy's periods of 26 Aug 2026), destroyed on
/// the day the dashboard would have destroyed it: right-to-work evidence two years after the person
/// left, in one step; everything else a person sent three years after they left and three more in
/// the archive; a company's forms the same six years from its last form; a company vehicle form six
/// years after the vehicle came back; an upload whose form was never sent eighteen months after it
/// arrived; a link nobody used a year after it died. A clock starts only on a recorded date — a
/// leaver with no date is reported, never clocked — and a date recorded before a form was sent
/// belongs to an earlier engagement: a person who comes back is not destroyed on their old clock.
/// </summary>
public static class FormRetention
{
    public const int RightToWorkMonths = 24;
    public const int KeptMonths = 72;
    public const int AbandonedUploadMonths = 18;
    public const int UnusedLinkMonths = 12;

    public static DateOnly? DestroyOn(
        FormDefinition form, DateOnly sentOn, DateOnly? engagementEndedOn, DateOnly? vehicleReturnedOn, DateOnly lastSubmittedOn)
    {
        var isRightToWork = form.Store == FormEvidenceStore.RightToWork;
        var isACompanysForm = form.FilingKind == FormFilingKind.Company;
        var isAVehicleForm = form.Slug == FormSlugs.CompanyVehicle;
        var endedOn = OnOrAfter(engagementEndedOn, sentOn);
        var vehicleGoneOn = OnOrAfter(vehicleReturnedOn, sentOn) ?? endedOn;
        if (isRightToWork) return endedOn?.AddMonths(RightToWorkMonths);
        if (isACompanysForm) return lastSubmittedOn.AddMonths(KeptMonths);
        if (isAVehicleForm) return vehicleGoneOn?.AddMonths(KeptMonths);
        return endedOn?.AddMonths(KeptMonths);
    }

    public static DateOnly? CheckDestroyOn(DateOnly? engagementEndedOn) => engagementEndedOn?.AddMonths(RightToWorkMonths);

    public static DateOnly? CertificateDestroyOn(DateOnly? endedOn) => endedOn?.AddMonths(KeptMonths);

    private static DateOnly? OnOrAfter(DateOnly? date, DateOnly sentOn) => date is { } day && day >= sentOn ? day : null;
}
