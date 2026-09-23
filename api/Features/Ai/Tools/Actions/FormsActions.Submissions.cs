using Jewel.JPMS.Api.Features.Forms.Office.Submissions;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class FormsActions
{
    private static AiAction SetFormSubmissionStatusAction() => new AiAction(
        Name: "set_form_submission_status",
        Area: Area,
        Description: "Moves a form that came in to New, InProgress or Handled — where the office has got to with it.",
        CommandType: typeof(SetFormSubmissionStatus),
        ResultType: typeof(FormSubmission),
        AuthorisationType: typeof(SetFormSubmissionStatusAuthorisation),
        ValidationType: typeof(SetFormSubmissionStatusValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "HandledByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "formSubmissionId comes from list_form_submissions. A destroyed form cannot be moved.");

    private static AiAction FileFormToDirectoryAction() => new AiAction(
        Name: "file_form_to_directory",
        Area: Area,
        Description: "Files a Sub-Contractor Questionnaire's or an Insurance Update's certificates onto a directory company's "
            + "compliance record, each with its kind, expiry and — for public liability — the cover in pounds, so the renewal is "
            + "chased and the 5m check reads it like any other certificate. An insurance update is handled once filed.",
        CommandType: typeof(FileFormToDirectory),
        ResultType: typeof(FormDirectoryFiling),
        AuthorisationType: typeof(FileFormToDirectoryAuthorisation),
        ValidationType: typeof(FileFormToDirectoryValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "FiledByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "Read get_form_submission first: files[].formUploadId are the ids, and filingSuggestions[] carry the kind, expiry "
            + "and cover the form states. subcontractorId comes from search_directory — confirm the company with the user; an ID "
            + "photo has no place on a company's compliance record.",
        RequiresConfirmation: true);

    private static AiAction RecordFormFolderDatesAction() => new AiAction(
        Name: "record_form_folder_dates",
        Area: Area,
        Description: "Records the day a person's engagement ended and the day a company vehicle came back — the dates that start "
            + "the clocks after which their forms and files are destroyed (right-to-work evidence two years on, the rest six). "
            + "Nothing is destroyed for a leaver with no date; a date long enough ago destroys them that night, so read the "
            + "date back to the user before the yes. Only someone who may read every form in the folder can set it.",
        CommandType: typeof(RecordFormFolderDates),
        ResultType: typeof(FormFolder),
        AuthorisationType: typeof(RecordFormFolderDatesAuthorisation),
        ValidationType: typeof(RecordFormFolderDatesValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "RecordedByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "formFolderId comes from list_form_folders. Both dates are sent every time; a blank one clears it.",
        RequiresConfirmation: true);
}
