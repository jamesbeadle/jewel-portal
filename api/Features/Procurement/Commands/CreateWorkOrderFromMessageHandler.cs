using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Procurement.Attachments;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Raises the work order from a tagged email and links the email to it. The order is created by the
// SAME handler as a manually raised order (numbering, draft semantics, cost-code master guard —
// one set of rules, whichever door the order came in through), then the originating email is
// tagged to it through the shared record-link path, exactly like CreateBidPackageFromMessage.
// The order is persisted first because the link path resolves the record from the database;
// a link failure therefore throws with the order already saved — same trade-off as the bid
// package equivalent, and the email stays in the queue to retry against the existing order.
// The one link failure that is a DECISION rather than a fault — the cross-pathway confirm — is
// therefore PRE-FLIGHTED against the email's current categories before anything persists
// (CrossPathwayGuard, 2026-08-22): rejecting after the order existed is what produced duplicate
// draft orders, one per retry, with a red error the triager couldn't confirm past.
//
// AttachmentIds are the email attachments the triager ticked to keep on the order as record
// keeping (never sent to the supplier). Their bytes are downloaded from the mailbox BEFORE the
// order is created, so a vanished or unreadable attachment fails the whole apply cleanly instead
// of leaving a half-attached order behind; they are stored (blob + register row) once the order
// exists, before the email is linked.
public sealed class CreateWorkOrderFromMessageHandler
    : ICommandHandler<CreateWorkOrderFromMessage, WorkOrder>
{
    private readonly ICommandHandler<CreateManualWorkOrder, WorkOrder> createOrder;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;
    private readonly IIntakeMessageReader reader;
    private readonly IWorkOrderAttachmentStore attachmentStore;
    private readonly IMailboxGraphClient graph;
    private readonly JpmsContext context;

    public CreateWorkOrderFromMessageHandler(
        ICommandHandler<CreateManualWorkOrder, WorkOrder> createOrder,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link,
        IIntakeMessageReader reader,
        IWorkOrderAttachmentStore attachmentStore,
        IMailboxGraphClient graph,
        JpmsContext context)
    {
        this.createOrder = createOrder;
        this.link = link;
        this.reader = reader;
        this.attachmentStore = attachmentStore;
        this.graph = graph;
        this.context = context;
    }

    public async Task<WorkOrder> HandleAsync(CreateWorkOrderFromMessage command, CancellationToken cancellationToken)
    {
        // Pre-flight the cross-pathway confirm BEFORE anything persists: a work order files the
        // thread under Subcontractor, and a thread already filed elsewhere needs the triager's
        // explicit "File under both anyway" — asked now, while refusing still costs nothing.
        // (The link at the end re-checks with the same consent flag; it cannot disagree.)
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");
        Jewel.JPMS.Api.Features.RecordLinks.CrossPathwayGuard.EnsureConfirmed(
            snapshot.Categories, TriageCategories.BucketFor(RecordType.WorkOrder),
            command.AllowCrossPathway, "the new work order");

        // Fetch the ticked email attachments FIRST — before anything persists — so "that
        // attachment isn't there any more" is a clean refusal, not a half-attached order.
        var attachmentIds = (command.AttachmentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var fetched = new List<(string Name, string ContentType, byte[] Content)>();
        foreach (var attachmentId in attachmentIds)
        {
            var attachment = await reader.GetAttachmentAsync(command.MessageId, attachmentId, cancellationToken);
            if (attachment is null)
                throw new InvalidOperationException(
                    "Couldn't download one of the ticked attachments from the mailbox — it may have "
                    + "been removed, or it isn't a file. Untick it and apply again.");
            fetched.Add((attachment.Name, attachment.ContentType, attachment.Content));
        }

        var order = await createOrder.HandleAsync(
            new CreateManualWorkOrder(
                command.ProjectId,
                command.SubcontractorId,
                command.Title,
                command.Scope,
                command.RaisedByEmail,
                command.Lines,
                command.ProgrammeStart,
                command.TargetCompletion,
                command.ProgrammeNotes,
                SaveAsDraft: command.SaveAsDraft,
                DepositRequired: command.DepositRequired,
                DepositPercent: command.DepositPercent),
            cancellationToken);

        // Store the ticked attachments against the new order: bytes into the private container,
        // a register row each. Record keeping only — the purchase-order email ignores these.
        if (fetched.Count > 0)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var (name, contentType, content) in fetched)
            {
                var attachmentId = Guid.NewGuid().ToString("N");
                string blobRef;
                using (var stream = new MemoryStream(content, writable: false))
                {
                    blobRef = await attachmentStore.UploadAsync(
                        command.ProjectId, order.WorkOrderId, attachmentId,
                        string.IsNullOrWhiteSpace(name) ? "attachment" : name,
                        string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                        stream, cancellationToken);
                }

                context.WorkOrderAttachments.Add(new WorkOrderAttachmentEntity
                {
                    WorkOrderAttachmentId = attachmentId,
                    WorkOrderId = order.WorkOrderId,
                    ProjectId = command.ProjectId,
                    FileName = string.IsNullOrWhiteSpace(name) ? "attachment" : name,
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    FileSizeBytes = content.LongLength,
                    BlobRef = blobRef,
                    Source = (int)WorkOrderAttachmentSource.Email,
                    AddedAt = now,
                    AddedByEmail = command.RaisedByEmail
                });
            }
            await context.SaveChangesAsync(cancellationToken);
        }

        // Tag the originating email to the new order through the shared record-link path (verified by
        // read-back inside the handler). Throws if the email can't be read/tagged.
        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.WorkOrder, order.WorkOrderId, command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway,
                Scope: command.LinkScope),
            cancellationToken);

        return order;
    }
}
