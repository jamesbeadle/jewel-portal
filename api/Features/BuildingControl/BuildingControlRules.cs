using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.BuildingControl;

namespace Jewel.JPMS.Api.Features.BuildingControl;

/// <summary>
/// The one place the editable faces of a case and an inspection are checked and applied — shared
/// by create, update and the triage create-from-message, so the routes cannot drift apart (the
/// CalendarEventDetailsRules arrangement). Dates are normalised to midnight UTC on the way in
/// (the SiteClock convention: a UK-local calendar date stored as midnight UTC).
/// </summary>
internal static class BuildingControlRules
{
    // ---- Case ---------------------------------------------------------------------------------

    public static IReadOnlyList<string> CaseProblems(BuildingControlCaseDetails details)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(details.BodyName)) errors.Add("Name the building control body.");
        if (details.BodyName is { Length: > 256 }) errors.Add("Body name must be 256 characters or fewer.");
        if (details.BodyReference is { Length: > 128 }) errors.Add("Body reference must be 128 characters or fewer.");
        if (details.ContactName is { Length: > 256 }) errors.Add("Contact name must be 256 characters or fewer.");
        if (details.ContactEmail is { Length: > 256 }) errors.Add("Contact email must be 256 characters or fewer.");
        if (details.ContactPhone is { Length: > 64 }) errors.Add("Contact phone must be 64 characters or fewer.");
        if (details.Notes is { Length: > 4096 }) errors.Add("Notes must be 4096 characters or fewer.");
        if (details.NoticeSubmittedOn is { } submitted && details.AcceptedOn is { } accepted
            && AsCalendarDate(accepted) < AsCalendarDate(submitted))
            errors.Add("Accepted date can't be before the notice was submitted.");
        return errors;
    }

    public static void Apply(BuildingControlCaseEntity entity, BuildingControlCaseDetails details)
    {
        entity.Regime = (int)details.Regime;
        entity.BodyName = details.BodyName.Trim();
        entity.BodyReference = details.BodyReference?.Trim() ?? "";
        entity.ContactName = details.ContactName?.Trim() ?? "";
        entity.ContactEmail = details.ContactEmail?.Trim() ?? "";
        entity.ContactPhone = details.ContactPhone?.Trim() ?? "";
        entity.NoticeSubmittedOn = AsCalendarDate(details.NoticeSubmittedOn);
        entity.AcceptedOn = AsCalendarDate(details.AcceptedOn);
        entity.Notes = details.Notes?.Trim() ?? "";
    }

    /// <summary>Whether a case still counts as the project's working case — the "one active case
    /// per project" rule reads this: only a lapsed or completion-certified case may be succeeded.</summary>
    public static bool IsActive(BuildingControlCaseEntity entity) =>
        (BuildingControlCaseStatus)entity.Status
            is not (BuildingControlCaseStatus.Lapsed or BuildingControlCaseStatus.CompletionCertified);

    // ---- Inspection ---------------------------------------------------------------------------

    public static IReadOnlyList<string> InspectionProblems(BuildingControlInspectionDetails details)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(details.StageName)) errors.Add("Name the inspection stage.");
        if (details.StageName is { Length: > 256 }) errors.Add("Stage name must be 256 characters or fewer.");
        if (details.OutcomeNotes is { Length: > 2048 }) errors.Add("Outcome notes must be 2048 characters or fewer.");
        if (details.InspectorName is { Length: > 256 }) errors.Add("Inspector name must be 256 characters or fewer.");
        return errors;
    }

    public static void Apply(BuildingControlInspectionEntity entity, BuildingControlInspectionDetails details)
    {
        entity.StageName = details.StageName.Trim();
        entity.BookedFor = AsCalendarDate(details.BookedFor);
        entity.InspectedAt = AsCalendarDate(details.InspectedAt);
        entity.OutcomeNotes = details.OutcomeNotes?.Trim() ?? "";
        entity.InspectorName = details.InspectorName?.Trim() ?? "";
    }

    /// <summary>
    /// One move along the inspection ladder. Moving to Inspected/Passed/ActionsRequired stamps
    /// InspectedAt (today) when the visit date isn't already recorded; moving back to
    /// Planned/Booked clears it — an unhappened visit has no visit date.
    /// </summary>
    public static void ApplyStatus(BuildingControlInspectionEntity entity, BuildingControlInspectionStatus status)
    {
        entity.Status = (int)status;
        switch (status)
        {
            case BuildingControlInspectionStatus.Planned:
            case BuildingControlInspectionStatus.Booked:
                entity.InspectedAt = null;
                break;
            case BuildingControlInspectionStatus.Inspected:
            case BuildingControlInspectionStatus.Passed:
            case BuildingControlInspectionStatus.ActionsRequired:
                entity.InspectedAt ??= AsCalendarDate(DateTimeOffset.UtcNow);
                break;
        }
    }

    /// <summary>The status a NEW stage starts at: Booked when a date is already agreed, else Planned.</summary>
    public static BuildingControlInspectionStatus StatusOnAdd(BuildingControlInspectionDetails details) =>
        details.BookedFor is null ? BuildingControlInspectionStatus.Planned : BuildingControlInspectionStatus.Booked;

    // ---- Attachments --------------------------------------------------------------------------

    /// <summary>The kind a copied-off email file lands as when the caller doesn't say: images are
    /// site photos, PDFs are the inspector's report, anything else is filed Other and re-kinded
    /// by hand if it matters.</summary>
    public static BuildingControlAttachmentKind InferKind(string contentType, string fileName)
    {
        if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
            return BuildingControlAttachmentKind.Photo;
        if (string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
            return BuildingControlAttachmentKind.SiteInspectionReport;
        return BuildingControlAttachmentKind.Other;
    }

    /// <summary>The calendar day the caller named, as midnight UTC — date part only, whatever
    /// offset or time-of-day the serialized value arrived with (CalendarEventDetailsRules rule).</summary>
    public static DateTimeOffset? AsCalendarDate(DateTimeOffset? value) =>
        value is { } v ? new DateTimeOffset(v.Date, TimeSpan.Zero) : null;

    public static DateTimeOffset AsCalendarDate(DateTimeOffset value) =>
        new(value.Date, TimeSpan.Zero);
}
