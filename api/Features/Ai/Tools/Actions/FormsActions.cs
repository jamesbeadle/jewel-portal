using Jewel.JPMS.Api.Features.Forms.Office.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

/// <summary>
/// The forms new starters and sub-contractors fill in for Jewel Bespoke Build (ported from Jeremy's
/// forms dashboard, 2026-09-23): every button on the /forms screens is here too. Anything that emails a
/// stranger is confirm-first; the reads that hand over the ids are list_form_submissions,
/// list_form_packs, list_form_invites and the register lists.
/// </summary>
internal sealed partial class FormsActions : IAiActionSource
{
    public const string Area = "Forms";

    public IEnumerable<AiAction> Build() => new[]
    {
        SendFormInviteAction(), ResendFormInviteAction(), CancelFormInviteAction(),
        SendFormPackAction(), ChaseFormPackAction(), CancelFormPackAction(),
        SetFormSubmissionStatusAction(), FileFormToDirectoryAction(), FileQuizToDirectoryAction(), RecordFormFolderDatesAction(),
        RecordACheckAction(), ConfirmACheckAction(),
        AcceptTrainingCertificateAction(), SetTrainingRecordDetailsAction(),
        ResolveAWorkstationAction(), RecordDrivingLicenceCheckAction()
    };

    private static AiAction SendFormInviteAction() => new AiAction(
        Name: "send_form_invite",
        Area: Area,
        Description: "SENDS EMAIL to one named person with a one-time link to one form. The link is theirs alone and "
            + "works once; days is how long it lasts (3, 7, 14 or 30; seven unless told). When the email cannot go the link is returned to send another way.",
        CommandType: typeof(SendFormInvite),
        ResultType: typeof(SentFormLink),
        AuthorisationType: typeof(SendFormInviteAuthorisation),
        ValidationType: typeof(SendFormInviteValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "SentByEmail" },
        NameStamps: new[] { "SentByName" },
        Notes: "formSlug is one of starter, emergency, rtw, dse, vehicle, training, accident, subcontractor, insurance, "
            + "or one of the H&S officer's site checks (toolbox-talk, ladder-inspection, equipment-schedule, puwer-inspection, "
            + "site-incident, personnel-incident, first-aid-kit, fire-extinguishers), which open at their own address without a link. "
            + "For a new starter's whole set send_form_pack instead. Show the user who and which form first.",
        RequiresConfirmation: true);

    private static AiAction ResendFormInviteAction() => new AiAction(
        Name: "resend_form_invite",
        Area: Area,
        Description: "SENDS EMAIL: a fresh link with a fresh expiry for a form sent on its own, optionally to a corrected "
            + "address. The old link stops working, so a forwarded email cannot be used later.",
        CommandType: typeof(ResendFormInvite),
        ResultType: typeof(SentFormLink),
        AuthorisationType: typeof(ResendFormInviteAuthorisation),
        ValidationType: typeof(ResendFormInviteValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "SentByEmail" },
        NameStamps: new[] { "SentByName" },
        Notes: "formInviteId comes from list_form_invites.",
        RequiresConfirmation: true);

    private static AiAction CancelFormInviteAction() => new AiAction(
        Name: "cancel_form_invite",
        Area: Area,
        Description: "Stops a form's one-time link working. Anything the person already sent stays on record.",
        CommandType: typeof(CancelFormInvite),
        ResultType: typeof(FormInvite),
        AuthorisationType: typeof(CancelFormInviteAuthorisation),
        ValidationType: typeof(CancelFormInviteValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: Array.Empty<string>(),
        NameStamps: Array.Empty<string>(),
        Notes: "formInviteId comes from list_form_invites.",
        RequiresConfirmation: true);
}
