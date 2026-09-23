using Jewel.JPMS.Api.Features.Forms.Registers;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class FormsActions
{
    private static AiAction AcceptTrainingCertificateAction() => new AiAction(
        Name: "accept_training_certificate",
        Area: Area,
        Description: "Accepts a Training Certificate form onto the training register with the person, course and dates the office "
            + "checked, so the holder is asked for the renewal a month before it lapses. The form is handled.",
        CommandType: typeof(AcceptTrainingCertificate),
        ResultType: typeof(TrainingRecord),
        AuthorisationType: typeof(AcceptTrainingCertificateAuthorisation),
        ValidationType: typeof(AcceptTrainingCertificateValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "AcceptedByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "Read get_form_submission first: trainingSuggestion carries the name, course and dates the form states. Spell the "
            + "person as on their other certificates (list_training_records) so their register reads as one person.");

    private static AiAction SetTrainingRecordDetailsAction() => new AiAction(
        Name: "set_training_record_details",
        Area: Area,
        Description: "Corrects a certificate's email (where its renewal is chased) and its expiry, or ends it when the person "
            + "leaves, which stops the chasing and starts the six years after which it is destroyed.",
        CommandType: typeof(SetTrainingRecordDetails),
        ResultType: typeof(TrainingRecord),
        AuthorisationType: typeof(SetTrainingRecordDetailsAuthorisation),
        ValidationType: typeof(SetTrainingRecordDetailsValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: Array.Empty<string>(),
        NameStamps: Array.Empty<string>(),
        Notes: "trainingRecordId comes from list_training_records. A full write of the three: a blank one is cleared. An end "
            + "date six years gone destroys the certificate that night.",
        RequiresConfirmation: true);

    private static AiAction ResolveAWorkstationAction() => new AiAction(
        Name: "resolve_workstation_action",
        Area: Area,
        Description: "Closes one thing to sort out from a workstation assessment as Fixed, or as Accepted with the reason it "
            + "stands. When the last one is closed the assessment is handled.",
        CommandType: typeof(ResolveWorkstationAction),
        ResultType: typeof(WorkstationAction),
        AuthorisationType: typeof(ResolveWorkstationActionAuthorisation),
        ValidationType: typeof(ResolveWorkstationActionValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "ResolvedByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "workstationActionId comes from list_workstation_actions.");

    private static AiAction RecordDrivingLicenceCheckAction() => new AiAction(
        Name: "record_driving_licence_check",
        Area: Area,
        Description: "Records the office's check of a Company Vehicle Form — the DVLA record viewed with the person's check code "
            + "and whether they meet the insurance criteria — and PERMANENTLY deletes the licence photograph and withholds the "
            + "driving-record answers and the check code, as the form's privacy paragraph promised. There is no undo.",
        CommandType: typeof(RecordDrivingLicenceCheck),
        ResultType: typeof(DrivingLicenceCheck),
        AuthorisationType: typeof(RecordDrivingLicenceCheckAuthorisation),
        ValidationType: typeof(RecordDrivingLicenceCheckValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "CheckedByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "formSubmissionId comes from list_form_submissions (formSlug vehicle). Only the outcome is kept; never repeat the "
            + "detail of an offence in the note.",
        RequiresConfirmation: true);
}
