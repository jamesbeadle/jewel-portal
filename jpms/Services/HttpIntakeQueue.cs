using System.Net.Http.Json;
using Jewel.JPMS.Contracts.Closeout;
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
    // Bounds a single uploaded attachment; the server enforces the same cap plus a combined one.
    private const long MaxUploadBytes = 25_000_000;

    private readonly IQueryClient queries;
    private readonly ICommandSender commands;
    private readonly HttpClient httpClient;

    public HttpIntakeQueue(IQueryClient queries, ICommandSender commands, HttpClient httpClient)
    {
        this.queries = queries;
        this.commands = commands;
        this.httpClient = httpClient;
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

    public Task<Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome> SendComposedEmailAsync(
        Jewel.JPMS.Contracts.MailboxCompose.SendMailboxEmail command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public async Task<Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome> SendComposedEmailAsync(
        Jewel.JPMS.Contracts.MailboxCompose.SendMailboxEmail command,
        IReadOnlyList<(string PartName, Microsoft.AspNetCore.Components.Forms.IBrowserFile File)> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
            return await SendComposedEmailAsync(command, cancellationToken);

        using var content = new System.Net.Http.MultipartFormDataContent();
        content.Add(new System.Net.Http.StringContent(
            System.Text.Json.JsonSerializer.Serialize(command, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))),
            "command");
        foreach (var (partName, file) in files)
        {
            var fileContent = new System.Net.Http.StreamContent(file.OpenReadStream(MaxUploadBytes, cancellationToken));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
            content.Add(fileContent, partName, file.Name);
        }

        var response = await httpClient.PostAsync("api/mailbox/compose", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Same contract as the command sender: surface the server's own message verbatim.
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CommandFailedException(
                string.IsNullOrWhiteSpace(body) ? $"The send failed ({(int)response.StatusCode})." : body.Trim('"'));
        }

        var outcome = await response.Content.ReadFromJsonAsync<Jewel.JPMS.Contracts.MailboxCompose.ComposeOutcome>(
            cancellationToken: cancellationToken);
        return outcome ?? throw new CommandFailedException("The send returned no outcome.");
    }

    public Task<IReadOnlyList<LinkableRecord>> ListLinkableRecordsAsync(string projectId, RecordType type, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListLinkableRecords(projectId, type), cancellationToken);

    public Task<Acknowledgement> LinkMessageToRecordAsync(string messageId, string? internetMessageId, RecordType type, string recordId, string? pathway = null, bool allowCrossPathway = false, LinkThreadScope scope = LinkThreadScope.ThreadBehindAnchor, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new LinkMessageToRecord(messageId, type, recordId, internetMessageId, pathway, allowCrossPathway, scope), cancellationToken);

    public Task<BidPackage> CreateBidPackageFromMessageAsync(CreateBidPackageFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<WorkOrder> CreateWorkOrderFromMessageAsync(CreateWorkOrderFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<IReadOnlyList<TodoItem>> CreateTodoItemsFromMessageAsync(CreateTodoItemsFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<Defect> CreateDefectFromMessageAsync(CreateDefectFromMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);
}
