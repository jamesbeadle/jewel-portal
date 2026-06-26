using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class PostRequestMessageHandler : ICommandHandler<PostRequestMessage, RequestMessage>
{
    private readonly JpmsContext context;
    public PostRequestMessageHandler(JpmsContext context) { this.context = context; }

    public async Task<RequestMessage> HandleAsync(PostRequestMessage command, CancellationToken cancellationToken)
    {
        var entity = new RequestMessageEntity
        {
            MessageId = RequestsIdentifierFactory.Next(),
            RequestId = command.RequestId,
            AuthorEmail = command.AuthorEmail,
            AuthorName = command.AuthorName,
            Body = command.Body,
            Visibility = (int)command.Visibility,
            PostedAt = DateTimeOffset.UtcNow
        };
        context.RequestMessages.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
