using Jewel.JPMS.Api.Features.ArchitectInstructions;
using Jewel.JPMS.Api.Features.BuildingControl;
using Jewel.JPMS.Api.Features.BuildingControl.Attachments;
using Jewel.JPMS.Api.Features.BuildingControl.Commands;
using Jewel.JPMS.Api.Features.Mobilisation.Commands;
using Jewel.JPMS.Api.Features.ProjectContracts;
using Jewel.JPMS.Api.Features.ProjectContracts.Commands;
using Jewel.JPMS.Api.Features.Projects.Commands;
using Jewel.JPMS.Api.Features.Projects.Contacts;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.TenderEnquiries;
using Jewel.JPMS.Api.Features.TenderEnquiries.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ArchitectInstructions;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Contracts.ProjectContracts;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.Ai.Tools.Actions;

internal sealed partial class ProjectsAndTendersActions
{
    private static IEnumerable<AiAction> ArchitectInstructionsActions() => new AiAction[]
    {
        new AiAction(
            Name: "import_architect_instruction_from_message",
            Area: "Architect instructions",
            Description: "Files an Architect's Instruction FROM a mailbox email: the named "
                + "attachment becomes the instruction's document, and the instruction can link to "
                + "the variations it covers as it is filed. An instruction is what an Awaiting-AI "
                + "variation is waiting for.",
            CommandType: typeof(ImportArchitectInstructionFromMessage),
            ResultType: typeof(ArchitectInstruction),
            AuthorisationType: typeof(ImportArchitectInstructionFromMessageAuthorisation),
            ValidationType: null,
            VisibleTo: ArchitectInstructionRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "messageId and attachmentId come from get_mailbox_message or read_record_emails. "
                + "Give a reference OR a title — either suffices. One instruction routinely covers "
                + "several variations; variationOrderIds links them now, "
                + "link_architect_instruction_to_variation links more later."),

        new AiAction(
            Name: "update_architect_instruction",
            Area: "Architect instructions",
            Description: "Corrects a filed instruction's details — reference, title, notes, "
                + "instructed date. The stored document is never touched.",
            CommandType: typeof(UpdateArchitectInstruction),
            ResultType: typeof(ArchitectInstruction),
            AuthorisationType: typeof(UpdateArchitectInstructionAuthorisation),
            ValidationType: null,
            VisibleTo: ArchitectInstructionRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            Notes: "All four fields are replaced together — read the register first "
                + "(list_architect_instructions) and resend what should not change."),

        new AiAction(
            Name: "link_architect_instruction_to_variation",
            Area: "Architect instructions",
            Description: "Links an instruction to a variation it covers — what moves an "
                + "Awaiting-AI variation's evidence into place. Linking twice is a no-op.",
            CommandType: typeof(LinkArchitectInstructionToVariation),
            ResultType: typeof(ArchitectInstruction),
            AuthorisationType: typeof(LinkArchitectInstructionToVariationAuthorisation),
            ValidationType: null,
            VisibleTo: ArchitectInstructionRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "unlink_architect_instruction_from_variation",
            Area: "Architect instructions",
            Description: "Removes an instruction-to-variation link. The instruction and the "
                + "variation both survive.",
            CommandType: typeof(UnlinkArchitectInstructionFromVariation),
            ResultType: typeof(ArchitectInstruction),
            AuthorisationType: typeof(UnlinkArchitectInstructionFromVariationAuthorisation),
            ValidationType: null,
            VisibleTo: ArchitectInstructionRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>()),

        new AiAction(
            Name: "delete_architect_instruction",
            Area: "Architect instructions",
            Description: "Permanently deletes an instruction, its variation links and its stored "
                + "document. There is no undo; the linked variations survive untouched.",
            CommandType: typeof(DeleteArchitectInstruction),
            ResultType: typeof(Acknowledgement),
            AuthorisationType: typeof(DeleteArchitectInstructionAuthorisation),
            ValidationType: null,
            VisibleTo: ArchitectInstructionRoles.AllowedToManage,
            EmailStamps: Array.Empty<string>(),
            NameStamps: Array.Empty<string>(),
            RequiresConfirmation: true,
            Notes: "Confirm with the user WHICH instruction, by reference and title, before calling."),
    };
}
