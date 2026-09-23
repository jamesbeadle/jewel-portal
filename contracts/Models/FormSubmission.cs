namespace Jewel.JPMS.Models;

/// <summary>Where the office has got to with a form that came in. Destroyed is the retention sweep's tombstone.</summary>
public enum FormSubmissionStatus
{
    New = 0,
    InProgress = 1,
    Handled = 2,
    Destroyed = 3
}

/// <summary>
/// One completed form as the office lists it. SubmitterName is the person the one-time link was
/// sent to when there was one, else the name typed; FilingName is the person or company everything
/// they send is filed under, across every form.
/// </summary>
public sealed record FormSubmission(
    string FormSubmissionId,
    string FormSlug,
    JewelCompany Company,
    string SubmitterName,
    string FilingName,
    string? FormFolderId,
    bool IsVerifiedLink,
    string SentToEmail,
    string SentByName,
    string? FormPackId,
    FormSubmissionStatus Status,
    DateTimeOffset SubmittedAt,
    string HandledByEmail,
    DateTimeOffset? HandledAt);

/// <summary>A file sent with a form. A deleted file keeps its row, with when and why, so the destruction is on record.</summary>
public sealed record FormUploadedFile(
    string FormUploadId,
    string QuestionKey,
    string FileName,
    string ContentType,
    long Size,
    DateTimeOffset? DeletedAt,
    string DeletionReason);

/// <summary>
/// A submission's answers by question key, its files, and the keys this reader is not shown until
/// they reveal them (the emergency form's health answers).
/// </summary>
public sealed record FormSubmissionView(
    FormSubmission Submission,
    IReadOnlyDictionary<string, string> Answers,
    IReadOnlyList<FormUploadedFile> Files,
    IReadOnlyList<string> WithheldKeys);

/// <summary>
/// The person or company everything they send is filed under. The dates start the retention
/// clocks: the engagement's end for a person's forms, the vehicle's return for their licence details.
/// </summary>
public sealed record FormFolder(
    string FormFolderId,
    string Name,
    FormFilingKind Kind,
    JewelCompany Company,
    DateOnly? EngagementEndedOn,
    DateOnly? VehicleReturnedOn,
    DateTimeOffset LastSubmittedAt,
    int SubmissionCount);

/// <summary>A person's latest emergency contact, laid out for someone on site who needs it now.</summary>
public sealed record EmergencyContactCard(
    string FormSubmissionId,
    string PersonName,
    JewelCompany Company,
    string ContactName,
    string Relationship,
    string ContactPhone,
    string ContactEmail,
    string ContactAddress,
    DateTimeOffset SubmittedAt,
    bool HasHealthAnswers,
    bool IsVerifiedLink);

/// <summary>What filing a questionnaire or insurance update to a directory company put on its compliance record.</summary>
public sealed record FormDirectoryFiling(
    string FormSubmissionId,
    string SubcontractorId,
    string CompanyName,
    IReadOnlyList<string> FiledKinds);
