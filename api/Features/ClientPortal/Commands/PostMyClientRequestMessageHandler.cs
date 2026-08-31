using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Contracts.ClientPortal;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ClientPortal.Commands;

/// <summary>The client adds to a request's shared thread. Visibility is always Shared — a client
/// session can neither write an internal note nor reply to one it can't see.</summary>
public sealed class PostMyClientRequestMessageHandler
    : ICommandHandler<PostMyClientRequestMessage, RequestMessage>
{
    private readonly JpmsContext context;
    public PostMyClientRequestMessageHandler(JpmsContext context) { this.context = context; }

    public async Task<RequestMessage> HandleAsync(
        PostMyClientRequestMessage command, CancellationToken cancellationToken)
    {
        var isMine = await ClientProjects.OwnsRequestAsync(
            context, command.ClientId, command.RequestId, cancellationToken);
        if (!isMine) throw new InvalidOperationException("This request is not available.");

        await GuardParentAsync(command, cancellationToken);

        var entity = new RequestMessageEntity
        {
            MessageId = RequestsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AuthorEmail = command.AuthorEmail,
            AuthorName = command.AuthorName,
            Body = command.Body,
            Visibility = (int)MessageVisibility.Shared,
            PostedAt = DateTimeOffset.UtcNow,
            ParentMessageId = command.ParentMessageId
        };
        context.RequestMessages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // The parent must be a message the client can actually see: a typed, shared message on the
    // same request. Internal notes and email legs both fail this check.
    private async Task GuardParentAsync(PostMyClientRequestMessage command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ParentMessageId)) return;
        var parentIsVisible = await context.RequestMessages
            .AsNoTracking()
            .AnyAsync(row => row.MessageId == command.ParentMessageId
                && row.RequestId == command.RequestId
                && row.Visibility == (int)MessageVisibility.Shared
                && row.Direction == (int)MessageDirection.System, cancellationToken);
        if (!parentIsVisible)
            throw new InvalidOperationException("The message being replied to is not on this request.");
    }
}
