using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.SiteInstructions;

namespace Jewel.JPMS.Api.Features.SiteInstructions.Commands;

// Raises the site instruction from a tagged email and links the email to it. The instruction is
// created by the SAME handler as one raised on the project's Site Instructions page (numbering —
// one set of rules, whichever door it came in through), then the originating email is tagged to
// it through the shared record-link path, exactly like CreateInventoryItemFromMessage /
// CreateDefectFromMessage. The record is persisted first because the link path resolves it from
// the database; a link failure therefore throws with the instruction already saved — same
// trade-off as the other from-message commands, and the email stays in the queue to retry
// against the existing instruction (now in the Tagging list).
public sealed class CreateSiteInstructionFromMessageHandler
    : ICommandHandler<CreateSiteInstructionFromMessage, SiteInstruction>
{
    private readonly ICommandHandler<AddSiteInstruction, SiteInstruction> add;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public CreateSiteInstructionFromMessageHandler(
        ICommandHandler<AddSiteInstruction, SiteInstruction> add,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    { this.add = add; this.link = link; }

    public async Task<SiteInstruction> HandleAsync(CreateSiteInstructionFromMessage command, CancellationToken cancellationToken)
    {
        var instruction = await add.HandleAsync(
            new AddSiteInstruction(
                command.ProjectId,
                command.Title,
                command.Instruction,
                command.Location),
            cancellationToken);

        // Tag the originating email to the new instruction through the shared record-link path
        // (verified by read-back inside the handler). Throws if the email can't be read/tagged.
        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.SiteInstruction, instruction.SiteInstructionId, command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway,
                Scope: command.Scope),
            cancellationToken);

        return instruction;
    }
}
