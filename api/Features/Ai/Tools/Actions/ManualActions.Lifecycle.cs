using Jewel.JPMS.Api.Features.Manual.Commands;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ManualActions
{
    private static IEnumerable<AiAction> LifecycleActions() => new AiAction[]
    {
        Transition("submit_manual_module_for_review", typeof(SubmitManualModuleForReview), "SubmittedByEmail", ManualRoles.AllowedToManage,
            "Sends a DRAFT module to its approver: Draft → In review. Refused while the module has no text, no owner or no approver."),
        Transition("approve_manual_module", typeof(ApproveManualModule), "ApprovedByEmail", ManualRoles.AllowedToApprove,
            "Approves a module in review: its working text becomes the published text at this version, the version is kept "
            + "whole, the previous version is superseded, and every reader acknowledges the new version afresh. "
            + "nextReviewAt (optional) sets the next review date."),
        Transition("return_manual_module_to_draft", typeof(ReturnManualModuleToDraft), "ReturnedByEmail", ManualRoles.AllowedToApprove,
            "Returns a module in review to its owner as a draft, with the reason, which is noted on the change summary."),
        Transition("revise_manual_module", typeof(ReviseManualModule), "RevisedByEmail", ManualRoles.AllowedToManage,
            "Opens the next version of an APPROVED module as a draft. The approved text stays published until the new "
            + "version is approved in its turn."),
        Transition("retire_manual_module", typeof(RetireManualModule), "RetiredByEmail", ManualRoles.AllowedToApprove,
            "Retires a module: it leaves every site view and its versions and acknowledgements stay as the record. "
            + "Confirm which module with the user first."),
        Transition("acknowledge_manual_module", typeof(AcknowledgeManualModule), "AcknowledgedByEmail", ManualRoles.AllowedToReadViews,
            "Records the signed-in user's own acknowledgement of a module's published version, with their typed name. "
            + "Once per version; a new approved version needs a fresh acknowledgement."),
    };

    private static AiAction Transition(string name, Type commandType, string emailStamp, RoleSet visibleTo, string description) =>
        new(
            Name: name,
            Area: Area,
            Description: description,
            CommandType: commandType,
            ResultType: commandType == typeof(AcknowledgeManualModule) ? typeof(ManualAcknowledgement) : typeof(ManualModule),
            AuthorisationType: typeof(ManualAuthorisationGate),
            ValidationType: typeof(ManualValidationGate),
            VisibleTo: visibleTo,
            EmailStamps: new[] { emailStamp },
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: name == "retire_manual_module",
            Notes: "manualModuleId comes from list_manual_modules.");
}
