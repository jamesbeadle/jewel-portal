using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.DocumentControl;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Marks a pending item Discarded. Nothing is deleted — the file and the email snapshot stay, the
// Discarded view lists it, and Restore puts it back in the queue.
public sealed class DiscardDocumentControlItemHandler
    : ICommandHandler<DiscardDocumentControlItem, DocumentControlItem>
{
    private readonly JpmsContext context;
    private readonly AuditActor actor;
    private readonly AuditTrail auditTrail;

    public DiscardDocumentControlItemHandler(JpmsContext context, AuditActor actor, AuditTrail auditTrail)
    {
        this.context = context; this.actor = actor; this.auditTrail = auditTrail;
    }

    public async Task<DocumentControlItem> HandleAsync(
        DiscardDocumentControlItem command, CancellationToken cancellationToken)
    {
        var item = await context.DocumentControlItems
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == command.DocumentControlItemId, cancellationToken)
            ?? throw new InvalidOperationException("That document is no longer in Document Triage.");
        if (item.Status == (int)DocumentControlStatus.Filed)
            throw new InvalidOperationException("A filed document can't be discarded — its destination record is the live copy.");

        item.Status = (int)DocumentControlStatus.Discarded;
        item.ResolvedBy = actor.Email;
        item.ResolvedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        await auditTrail.WriteAsync(
            AuditEventType.DocumentDiscarded,
            $"Discarded \"{item.FileName}\" in Document Triage",
            projectId: item.ProjectIdHint,
            emailMessageId: item.MessageId,
            internetMessageId: item.InternetMessageId,
            cancellationToken: cancellationToken);

        return item.ToModel();
    }
}
