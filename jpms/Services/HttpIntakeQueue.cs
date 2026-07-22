using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Todos;
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

    public Task<MailboxPage> ListInboxLiveAsync(string? cursor = null, int take = 25, bool newestFirst = false, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListInboxMessages(cursor, take, newestFirst), cancellationToken);

    public Task<MailboxPage> ListDiscardedLiveAsync(string? cursor = null, int take = 25, bool newestFirst = false, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListDiscardedMessages(cursor, take, newestFirst), cancellationToken);

    public Task<MailboxPage> ListTaggedLiveAsync(string? cursor = null, int take = 25, IReadOnlyList<string>? tags = null, bool newestFirst = false, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListTaggedMessages(cursor, take, tags, newestFirst), cancellationToken);

    public Task<MailboxPage> ListConversationLiveAsync(string conversationId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListConversationMessages(conversationId), cancellationToken);

    public Task<MailboxMessageDetail> GetMessageDetailAsync(string messageId, string? internetMessageId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetMailboxMessageDetail(messageId, internetMessageId), cancellationToken);

    public Task<TriageRecommendation> RecommendActionAsync(MailboxMessage message, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new RecommendTriageAction(
            message.Id, message.InternetMessageId, message.ConversationId,
            message.Subject, message.FromEmail, message.FromName), cancellationToken);

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

    public Task<ReplyInThreadOutcome> ReplyInThreadFromMessageAsync(ReplyInThreadFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<IReadOnlyList<LinkableRecord>> ListLinkableRecordsAsync(string projectId, RecordType type, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListLinkableRecords(projectId, type), cancellationToken);

    public Task<Acknowledgement> LinkMessageToRecordAsync(string messageId, string? internetMessageId, RecordType type, string recordId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new LinkMessageToRecord(messageId, type, recordId, internetMessageId), cancellationToken);

    public Task<Acknowledgement> SyncRecordThreadTagsAsync(RecordType type, string recordId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new SyncRecordThreadTags(type, recordId), cancellationToken);

    public Task<BidPackage> CreateBidPackageFromMessageAsync(CreateBidPackageFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<IReadOnlyList<TodoItem>> CreateTodoItemsFromMessageAsync(CreateTodoItemsFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);
}
