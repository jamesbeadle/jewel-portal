using Jewel.JPMS.Api.Features.Forms.Registers;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class FormsActions
{
    private static AiAction RecordACheckAction() => new AiAction(
        Name: "save_right_to_work_check",
        Area: Area,
        Description: "Records, or corrects, a right-to-work check on the register against the named person who carried it out — "
            + "the record that gives the statutory excuse. A pass needs all three confirmations, the date is never back- or "
            + "forward-dated, and time-limited permission needs its expiry. Recorded from a Right to Work form, that form is handled.",
        CommandType: typeof(SaveRightToWorkCheck),
        ResultType: typeof(RightToWorkCheck),
        AuthorisationType: typeof(SaveRightToWorkCheckAuthorisation),
        ValidationType: typeof(SaveRightToWorkCheckValidation),
        VisibleTo: FormRoleSets.RightToWorkReaders,
        EmailStamps: new[] { "RecordedByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "rightToWorkCheckId is null for a new check, else from list_right_to_work_checks — a correction sends every field. "
            + "The share code or IDSP reference is not read over the connector, so a correction on a route that needs one asks "
            + "the user for it or is made on the page. details.checkedByName is the person who saw the document, never the "
            + "assistant. The evidence file is attached on the page, not over the connector.",
        RequiresConfirmation: true);

    private static AiAction ConfirmACheckAction() => new AiAction(
        Name: "send_right_to_work_confirmation",
        Area: Area,
        Description: "SENDS EMAIL to the person a completed pass is about: when their check was done, by whom and by which route, "
            + "and since when they have been employed or engaged. Only a completed pass with an email on the record.",
        CommandType: typeof(SendRightToWorkConfirmation),
        ResultType: typeof(RightToWorkCheck),
        AuthorisationType: typeof(SendRightToWorkConfirmationAuthorisation),
        ValidationType: typeof(SendRightToWorkConfirmationValidation),
        VisibleTo: FormRoleSets.RightToWorkReaders,
        EmailStamps: new[] { "SentByEmail" },
        NameStamps: Array.Empty<string>(),
        Notes: "rightToWorkCheckId comes from list_right_to_work_checks; confirmedOn there says whether it has already gone.",
        RequiresConfirmation: true);
}
