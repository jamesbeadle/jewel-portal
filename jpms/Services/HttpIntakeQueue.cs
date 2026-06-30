using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpIntakeQueue : IIntakeQueue
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpIntakeQueue(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<MailboxPage> ListInboxLiveAsync(string? cursor = null, int take = 25, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListInboxMessages(cursor, take), cancellationToken);

    public Task<MailboxPage> ListDiscardedLiveAsync(string? cursor = null, int take = 25, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListDiscardedMessages(cursor, take), cancellationToken);

    public Task<MailboxPage> ListTaggedLiveAsync(string? cursor = null, int take = 25, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTaggedMessages(cursor, take), cancellationToken);

    public Task<MailboxMessageDetail> GetMessageDetailAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetMailboxMessageDetail(messageId, internetMessageId), cancellationToken);

    public Task<Acknowledgement> DiscardMessageAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DiscardMessage(messageId, internetMessageId), cancellationToken);

    public Task<Acknowledgement> RestoreMessageAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RestoreMessage(messageId, internetMessageId), cancellationToken);

    public Task<Acknowledgement> RemoveTagFromMessageAsync(string messageId, string? internetMessageId, string tag, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemoveTagFromMessage(messageId, internetMessageId, tag), cancellationToken);

    public Task<Acknowledgement> AssignMessageAsync(string messageId, string? internetMessageId, string requestId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new AssignMessageToRequest(messageId, requestId, internetMessageId), cancellationToken);

    public Task<Request> CreateRequestFromMessageAsync(CreateRequestFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);
}
