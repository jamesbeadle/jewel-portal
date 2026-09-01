using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Variations;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations.Commands;

public sealed class PostVariationOrderMessageHandler
    : ICommandHandler<PostVariationOrderMessage, VariationOrderMessage>
{
    private readonly JpmsContext context;
    public PostVariationOrderMessageHandler(JpmsContext context) { this.context = context; }

    public async Task<VariationOrderMessage> HandleAsync(
        PostVariationOrderMessage command, CancellationToken cancellationToken)
    {
        await GuardOrderExistsAsync(command, cancellationToken);
        await GuardParentAsync(command, cancellationToken);

        var entity = new VariationOrderMessageEntity
        {
            MessageId = VariationsIdentifierFactory.NextVariationOrderMessageId(),
            VariationOrderId = command.VariationOrderId,
            AuthorEmail = command.AuthorEmail,
            AuthorName = command.AuthorName,
            Body = command.Body,
            Visibility = (int)command.Visibility,
            PostedAt = DateTimeOffset.UtcNow,
            ParentMessageId = command.ParentMessageId
        };
        context.VariationOrderMessages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private async Task GuardOrderExistsAsync(PostVariationOrderMessage command, CancellationToken cancellationToken)
    {
        var orderExists = await context.VariationOrders
            .AsNoTracking()
            .AnyAsync(row => row.VariationOrderId == command.VariationOrderId, cancellationToken);
        if (!orderExists)
            throw new InvalidOperationException("This variation order no longer exists.");
    }

    private async Task GuardParentAsync(PostVariationOrderMessage command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ParentMessageId)) return;
        var parentIsOnThisOrder = await context.VariationOrderMessages
            .AsNoTracking()
            .AnyAsync(row => row.MessageId == command.ParentMessageId
                && row.VariationOrderId == command.VariationOrderId, cancellationToken);
        if (!parentIsOnThisOrder)
            throw new InvalidOperationException("The message being replied to is not on this variation order.");
    }
}
