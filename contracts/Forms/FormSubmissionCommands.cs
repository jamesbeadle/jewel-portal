using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

/// <summary>Moves a form that came in to New, In progress or Handled. HandledByEmail is stamped server-side.</summary>
public sealed record SetFormSubmissionStatus(
    string FormSubmissionId,
    FormSubmissionStatus Status,
    string HandledByEmail = "") : ICommand<FormSubmission>;

/// <summary>
/// The dates that start a person's or company's retention clocks: the day their engagement ended,
/// and the day a company vehicle came back. RecordedByEmail is stamped server-side.
/// </summary>
public sealed record RecordFormFolderDates(
    string FormFolderId,
    DateOnly? EngagementEndedOn,
    DateOnly? VehicleReturnedOn,
    string RecordedByEmail = "") : ICommand<FormFolder>;

/// <summary>
/// Files a questionnaire's or insurance update's certificates onto a directory company's
/// compliance record, with the expiry dates the form collected, so the renewal is chased.
/// </summary>
public sealed record FileFormToDirectory(
    string FormSubmissionId,
    string SubcontractorId,
    IReadOnlyList<FormUploadToFile> Files,
    string FiledByEmail = "") : ICommand<FormDirectoryFiling>;

/// <summary>One file from the form and what it is on the company's record: its kind, expiry and, for public liability, the cover.</summary>
public sealed record FormUploadToFile(string FormUploadId, string Kind, DateOnly? ExpiresOn, decimal? PublicLiabilityCover);
