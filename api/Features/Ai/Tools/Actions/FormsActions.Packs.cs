using Jewel.JPMS.Api.Features.Forms.Office.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class FormsActions
{
    private static AiAction SendFormPackAction() => new AiAction(
        Name: "send_form_pack",
        Area: Area,
        Description: "SENDS EMAIL to a new starter with ONE link to every form they owe, in the name of the company picked. "
            + "The portal decides the forms from engagedAs and the four answers: right to work and an emergency contact from "
            + "everybody, the HMRC starter checklist from an employee with no P45, the workstation assessment for screen work, "
            + "the vehicle form for a company vehicle, a training certificate where the role needs a ticket.",
        CommandType: typeof(SendFormPack),
        ResultType: typeof(SentFormPack),
        AuthorisationType: typeof(SendFormPackAuthorisation),
        ValidationType: typeof(SendFormPackValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "SentByEmail" },
        NameStamps: new[] { "SentByName" },
        Notes: "engagedAs is Employee or SelfEmployed. The link lasts fourteen days and a little longer each time it is used. "
            + "Say which forms the pack will hold before the user confirms.",
        RequiresConfirmation: true);

    private static AiAction ChaseFormPackAction() => new AiAction(
        Name: "chase_form_pack",
        Area: Area,
        Description: "SENDS EMAIL: the pack's link again as a reminder, with a fresh fourteen days on a new link; what is done "
            + "stays done and the old link stops working.",
        CommandType: typeof(ChaseFormPack),
        ResultType: typeof(SentFormPack),
        AuthorisationType: typeof(ChaseFormPackAuthorisation),
        ValidationType: typeof(ChaseFormPackValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: new[] { "SentByEmail" },
        NameStamps: new[] { "SentByName" },
        Notes: "formPackId comes from list_form_packs, which shows what is done and what is outstanding.",
        RequiresConfirmation: true);

    private static AiAction CancelFormPackAction() => new AiAction(
        Name: "cancel_form_pack",
        Area: Area,
        Description: "Stops a new starter's pack link working. The forms already sent stay on record.",
        CommandType: typeof(CancelFormPack),
        ResultType: typeof(FormPack),
        AuthorisationType: typeof(CancelFormPackAuthorisation),
        ValidationType: typeof(CancelFormPackValidation),
        VisibleTo: FormRoleSets.Office,
        EmailStamps: Array.Empty<string>(),
        NameStamps: Array.Empty<string>(),
        Notes: "formPackId comes from list_form_packs.",
        RequiresConfirmation: true);
}
