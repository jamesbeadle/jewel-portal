using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class LabourAndBackOfficeActions
{
    private static IEnumerable<AiAction> UsefulInformationActions() => new AiAction[]
    {
        new AiAction(
            Name: "add_useful_information_note",
            Area: "Useful information",
            Description: "Adds a Useful Information note to a project — internal reference "
                + "material such as door codes, key safe locations and site access notes, visible "
                + "to all staff on the project's Useful Information tab immediately. Never shown to "
                + "external logins. Recorded as created by the signed-in user.",
            CommandType: typeof(AddUsefulInformationNote),
            ResultType: typeof(UsefulInformationNote),
            AuthorisationType: typeof(AddUsefulInformationNoteAuthorisation),
            ValidationType: typeof(AddUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: new[] { "CreatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "projectId comes from list_projects."),

        new AiAction(
            Name: "update_useful_information_note",
            Area: "Useful information",
            Description: "Replaces a Useful Information note's title and body in one write — the "
                + "whole staff sees the new text immediately. Recorded as edited by the signed-in "
                + "user.",
            CommandType: typeof(UpdateUsefulInformationNote),
            ResultType: typeof(UsefulInformationNote),
            AuthorisationType: typeof(UpdateUsefulInformationNoteAuthorisation),
            ValidationType: typeof(UpdateUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: new[] { "UpdatedByEmail" },
            NameStamps: Array.Empty<string>(),
            Notes: "usefulInformationNoteId identifies the note. Both title and body are replaced "
                + "— read the current note first and carry forward what should not change."),

        new AiAction(
            Name: "delete_useful_information_note",
            Area: "Useful information",
            Description: "Deletes a Useful Information note permanently. There is no undo.",
            CommandType: typeof(DeleteUsefulInformationNote),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteUsefulInformationNoteAuthorisation),
            ValidationType: typeof(DeleteUsefulInformationNoteValidation),
            VisibleTo: UsefulInformationRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user which note, by title, before calling."),
    };
}
