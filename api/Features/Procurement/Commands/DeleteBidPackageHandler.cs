using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Deletes a bid package and everything that only exists under it: recipients, line items,
/// quotes and their lines, tender-document attachments (rows and blobs) and drawing links.
/// Mirrors DeleteVariationOrder's shape: idempotent on a missing id, refused while anything
/// carrying committed money references the record. Tagged mailbox emails are untouched —
/// correspondence outlives the record it was about.
/// </summary>
public sealed class DeleteBidPackageHandler : ICommandHandler<DeleteBidPackage, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly Attachments.IBidPackageAttachmentStore attachmentStore;
    private readonly ILogger<DeleteBidPackageHandler> logger;

    public DeleteBidPackageHandler(JpmsContext context,
        Attachments.IBidPackageAttachmentStore attachmentStore, ILogger<DeleteBidPackageHandler> logger)
    {
        this.context = context; this.attachmentStore = attachmentStore; this.logger = logger;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteBidPackage command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) return new Acknowledgement(command.BidPackageId); // already gone — nothing to do

        if (package.Status == (int)BidPackageStatus.Awarded)
            throw new InvalidOperationException(
                "An awarded bid package can't be deleted — its work order carries the committed money. Cancel the work order first.");

        // Belt and braces: a work order can name the package even in odd historical states.
        var instructed = await context.WorkOrders
            .AnyAsync(order => order.BidPackageId == package.BidPackageId, cancellationToken);
        if (instructed)
            throw new InvalidOperationException(
                "A work order references this bid package — cancel or reject it before deleting the package.");

        var recipients = await context.BidPackageRecipients
            .Where(recipient => recipient.BidPackageId == package.BidPackageId)
            .ToListAsync(cancellationToken);
        context.BidPackageRecipients.RemoveRange(recipients);

        var lineItems = await context.BidPackageLineItems
            .Where(line => line.BidPackageId == package.BidPackageId)
            .ToListAsync(cancellationToken);
        context.BidPackageLineItems.RemoveRange(lineItems);

        var quotes = await context.Quotes
            .Where(quote => quote.BidPackageId == package.BidPackageId)
            .ToListAsync(cancellationToken);
        var quoteIds = quotes.Select(quote => quote.QuoteId).ToList();
        var quoteLines = await context.QuoteLineItems
            .Where(line => quoteIds.Contains(line.QuoteId))
            .ToListAsync(cancellationToken);
        context.QuoteLineItems.RemoveRange(quoteLines);
        context.Quotes.RemoveRange(quotes);

        var drawingLinks = await context.BidPackageDrawings
            .Where(link => link.BidPackageId == package.BidPackageId)
            .ToListAsync(cancellationToken);
        context.BidPackageDrawings.RemoveRange(drawingLinks);

        var attachments = await context.BidPackageAttachments
            .Where(attachment => attachment.BidPackageId == package.BidPackageId)
            .ToListAsync(cancellationToken);
        context.BidPackageAttachments.RemoveRange(attachments);

        context.BidPackages.Remove(package);
        await context.SaveChangesAsync(cancellationToken);

        // Blob clean-up is best-effort AFTER the rows are gone: an orphaned blob is a pennies
        // problem, a deleted blob whose row survived a failed save would be a broken download.
        foreach (var attachment in attachments)
        {
            try { await attachmentStore.DeleteAsync(attachment.BlobRef, cancellationToken); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Couldn't delete bid package attachment blob {BlobRef}.", attachment.BlobRef);
            }
        }

        return new Acknowledgement(command.BidPackageId);
    }
}
