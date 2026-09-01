using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;

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
    private readonly Jewel.JPMS.Api.Features.MailboxIntake.Graph.IMailboxGraphClient graph;

    public CreateDefectFromMessageHandler(
        ICommandHandler<RaiseDefect, Defect> raiseDefect,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link,
        Jewel.JPMS.Api.Features.MailboxIntake.Graph.IMailboxGraphClient graph)
    { this.raiseDefect = raiseDefect; this.link = link; this.graph = graph; }

    public async Task<Defect> HandleAsync(CreateDefectFromMessage command, CancellationToken cancellationToken)
    {
        // Pre-flight the cross-pathway confirm BEFORE the defect persists (CrossPathwayGuard,
        // 2026-08-22): a defect files the thread under Subcontractor, and rejecting after the
        // create left an orphaned defect behind and a red error the triager couldn't confirm past.
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");
        Jewel.JPMS.Api.Features.RecordLinks.CrossPathwayGuard.EnsureConfirmed(
            snapshot.Categories,
            Jewel.JPMS.Api.Features.MailboxIntake.Graph.TriageCategories.BucketFor(RecordType.Defect),
            command.AllowCrossPathway, "the new defect");

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
                AllowCrossPathway: command.AllowCrossPathway,
                Scope: command.Scope),
            cancellationToken);

        return defect;
    }
}
