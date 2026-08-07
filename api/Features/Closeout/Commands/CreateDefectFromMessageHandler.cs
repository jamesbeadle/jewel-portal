using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

// Raises the defect from a tagged email and links the email to it. The defect is created by the
// SAME handler as a manually raised defect (numbering, Open status — one set of rules, whichever
// door the defect came in through), then the originating email is tagged to it through the shared
// record-link path, exactly like CreateBidPackageFromMessage / CreateWorkOrderFromMessage.
// The defect is persisted first because the link path resolves the record from the database;
// a link failure therefore throws with the defect already saved — same trade-off as the other
// from-message commands, and the email stays in the queue to retry against the existing defect.
public sealed class CreateDefectFromMessageHandler
    : ICommandHandler<CreateDefectFromMessage, Defect>
{
    private readonly ICommandHandler<RaiseDefect, Defect> raiseDefect;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public CreateDefectFromMessageHandler(
        ICommandHandler<RaiseDefect, Defect> raiseDefect,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    { this.raiseDefect = raiseDefect; this.link = link; }

    public async Task<Defect> HandleAsync(CreateDefectFromMessage command, CancellationToken cancellationToken)
    {
        var defect = await raiseDefect.HandleAsync(
            new RaiseDefect(
                command.ProjectId,
                command.Description,
                command.Location,
                command.AssignedToEmail),
            cancellationToken);

        // Tag the originating email to the new defect through the shared record-link path (verified
        // by read-back inside the handler). Throws if the email can't be read/tagged.
        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.Defect, defect.DefectId, command.InternetMessageId,
                Scope: command.Scope),
            cancellationToken);

        return defect;
    }
}
