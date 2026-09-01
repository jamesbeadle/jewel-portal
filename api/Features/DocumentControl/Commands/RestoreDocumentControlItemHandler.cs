using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

// Returns a discarded item to the pending queue. Filed items stay filed — their destination record
// is the live copy, so "restoring" one would mean two truths.
public sealed class RestoreDocumentControlItemHandler
    : ICommandHandler<RestoreDocumentControlItem, DocumentControlItem>
{
    private readonly JpmsContext context;

    public RestoreDocumentControlItemHandler(JpmsContext context) { this.context = context; }

    public async Task<DocumentControlItem> HandleAsync(
        RestoreDocumentControlItem command, CancellationToken cancellationToken)
    {
        var item = await context.DocumentControlItems
            .FirstOrDefaultAsync(row => row.DocumentControlItemId == command.DocumentControlItemId, cancellationToken)
            ?? throw new InvalidOperationException("That document is no longer in Document Triage.");
        if (item.Status != (int)DocumentControlStatus.Discarded)
            throw new InvalidOperationException("Only a discarded document can be restored to the queue.");

        item.Status = (int)DocumentControlStatus.Pending;
        item.ResolvedBy = null;
        item.ResolvedAt = null;
        await context.SaveChangesAsync(cancellationToken);
        return item.ToModel();
    }
}
