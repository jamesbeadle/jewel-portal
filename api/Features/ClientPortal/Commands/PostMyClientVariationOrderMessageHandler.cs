using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Variations;
using Jewel.JPMS.Contracts.ClientPortal;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ClientPortal.Commands;

/// <summary>The client adds to a variation order's shared thread. Visibility is always Shared —
/// a client session can neither write an internal note nor reply to one it can't see.</summary>
public sealed class PostMyClientVariationOrderMessageHandler
    : ICommandHandler<PostMyClientVariationOrderMessage, VariationOrderMessage>
{
    private readonly JpmsContext context;
    public PostMyClientVariationOrderMessageHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderMessage> HandleAsync(
        PostMyClientVariationOrderMessage command, CancellationToken cancellationToken)
    {
        var isMine = await ClientProjects.OwnsVariationOrderAsync(
            context, command.ClientId, command.VariationOrderId, cancellationToken);
        if (!isMine) throw new InvalidOperationException("This variation order is not available.");

        await GuardParentAsync(command, cancellationToken);

        var entity = new VariationOrderMessageEntity
        {
            MessageId = VariationsIdentifierFactory.NextVariationOrderMessageId(),
            VariationOrderId = command.VariationOrderId,
            AuthorEmail = command.AuthorEmail,
            AuthorName = command.AuthorName,
            Body = command.Body,
            Visibility = (int)MessageVisibility.Shared,
            PostedAt = DateTimeOffset.UtcNow,
            ParentMessageId = command.ParentMessageId
        };
        context.VariationOrderMessages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // The parent must be a message the client can actually see: a shared message on the same
    // variation order. Internal notes fail this check.
    private async Task GuardParentAsync(PostMyClientVariationOrderMessage command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ParentMessageId)) return;
        var parentIsVisible = await context.VariationOrderMessages
            .AsNoTracking()
            .AnyAsync(row => row.MessageId == command.ParentMessageId
                && row.VariationOrderId == command.VariationOrderId
                && row.Visibility == (int)MessageVisibility.Shared, cancellationToken);
        if (!parentIsVisible)
            throw new InvalidOperationException("The message being replied to is not on this variation order.");
    }
}
