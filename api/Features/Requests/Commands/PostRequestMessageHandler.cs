using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageHandler : ICommandHandler<PostRequestMessage, RequestMessage>
{
    private readonly JpmsContext context;
    public PostRequestMessageHandler(JpmsContext context) { this.context = context; }

    public async Task<RequestMessage> HandleAsync(PostRequestMessage command, CancellationToken cancellationToken)
    {
        await GuardParentAsync(command, cancellationToken);

        var entity = new RequestMessageEntity
        {
            MessageId = RequestsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AuthorEmail = command.AuthorEmail,
            AuthorName = command.AuthorName,
            Body = command.Body,
            Visibility = (int)command.Visibility,
            PostedAt = DateTimeOffset.UtcNow,
            ParentMessageId = command.ParentMessageId
        };
        context.RequestMessages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // A reply must answer an in-app message on the SAME request. Email legs never match here —
    // their conversation ids are internet-message ids, not RequestMessages keys — so threading
    // stays between typed messages, as designed.
    private async Task GuardParentAsync(PostRequestMessage command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.ParentMessageId)) return;
        var parentIsOnThisRequest = await context.RequestMessages
            .AsNoTracking()
            .AnyAsync(row => row.MessageId == command.ParentMessageId
                && row.RequestId == command.RequestId, cancellationToken);
        if (!parentIsOnThisRequest)
            throw new InvalidOperationException("The message being replied to is not on this request.");
    }
}
