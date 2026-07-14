using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// HTTP surface for the live-read triage model: read the Inbox (the queue) and the General folder
/// (discarded) straight from the mailbox, and move a message between them. Message ids are passed in
/// the query string / body, never the route path, because Graph ids contain characters that don't
/// survive a URL path segment. All operations are gated to the triage roles.
/// </summary>
public sealed class MailboxTriageEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListInboxMessages, MailboxPage> listInbox;
    private readonly IQueryHandler<ListDiscardedMessages, MailboxPage> listDiscarded;
    private readonly IQueryHandler<ListTaggedMessages, MailboxPage> listTagged;
    private readonly IQueryHandler<ListConversationMessages, MailboxPage> listConversation;
    private readonly IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail> detail;
    private readonly IQueryHandler<RecommendTriageAction, TriageRecommendation> recommend;
    private readonly ICommandHandler<DiscardMessage, Acknowledgement> discard;
    private readonly ICommandHandler<RestoreMessage, Acknowledgement> restore;
    private readonly ICommandHandler<RemoveTagFromMessage, Acknowledgement> removeTag;
    private readonly ICommandHandler<AssignMessageToRequest, Acknowledgement> assign;
    private readonly ICommandHandler<CreateRequestFromMessage, Request> create;
    private readonly ICommandHandler<ReplyInThreadFromMessage, ReplyInThreadOutcome> replyInThread;

    public MailboxTriageEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListInboxMessages, MailboxPage> listInbox,
        IQueryHandler<ListDiscardedMessages, MailboxPage> listDiscarded,
        IQueryHandler<ListTaggedMessages, MailboxPage> listTagged,
        IQueryHandler<ListConversationMessages, MailboxPage> listConversation,
        IQueryHandler<GetMailboxMessageDetail, MailboxMessageDetail> detail,
        IQueryHandler<RecommendTriageAction, TriageRecommendation> recommend,
        ICommandHandler<DiscardMessage, Acknowledgement> discard,
        ICommandHandler<RestoreMessage, Acknowledgement> restore,
        ICommandHandler<RemoveTagFromMessage, Acknowledgement> removeTag,
        ICommandHandler<AssignMessageToRequest, Acknowledgement> assign,
        ICommandHandler<CreateRequestFromMessage, Request> create,
        ICommandHandler<ReplyInThreadFromMessage, ReplyInThreadOutcome> replyInThread)
    {
        this.users = users;
        this.listInbox = listInbox;
        this.listDiscarded = listDiscarded;
        this.listTagged = listTagged;
        this.listConversation = listConversation;
        this.detail = detail;
        this.recommend = recommend;
        this.discard = discard;
        this.restore = restore;
        this.removeTag = removeTag;
        this.assign = assign;
        this.create = create;
        this.replyInThread = replyInThread;
    }

    [Function(nameof(ListInboxMessages))]
    public async Task<IActionResult> Inbox(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/inbox")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var (cursor, take) = Paging(request);
        return new OkObjectResult(await listInbox.HandleAsync(new ListInboxMessages(cursor, take), request.HttpContext.RequestAborted));
    }

    [Function(nameof(ListDiscardedMessages))]
    public async Task<IActionResult> Discarded(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/discarded")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var (cursor, take) = Paging(request);
        return new OkObjectResult(await listDiscarded.HandleAsync(new ListDiscardedMessages(cursor, take), request.HttpContext.RequestAborted));
    }

    [Function(nameof(ListTaggedMessages))]
    public async Task<IActionResult> Tagged(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/tagged")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var (cursor, take) = Paging(request);
        // Filter tags arrive comma-separated in the "tags" query param (e.g. "JPMS/RFI-001,JPMS/Discarded").
        var tags = request.Query["tags"].ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var query = new ListTaggedMessages(cursor, take, tags.Length == 0 ? null : tags);
        return new OkObjectResult(await listTagged.HandleAsync(query, request.HttpContext.RequestAborted));
    }

    [Function(nameof(ListConversationMessages))]
    public async Task<IActionResult> Conversation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/conversation")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        // The conversation id travels in the query string like message ids do: Graph ids contain
        // characters that don't survive a URL path segment.
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        return new OkObjectResult(await listConversation.HandleAsync(new ListConversationMessages(id), request.HttpContext.RequestAborted));
    }

    [Function(nameof(GetMailboxMessageDetail))]
    public async Task<IActionResult> Detail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/message/detail")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        var imid = request.Query["imid"].ToString();
        var query = new GetMailboxMessageDetail(id, string.IsNullOrWhiteSpace(imid) ? null : imid);
        return new OkObjectResult(await detail.HandleAsync(query, request.HttpContext.RequestAborted));
    }

    // Ask Claude to read the email's thread and recommend the next triage action. Advisory read
    // only; returns { available: false } when the AI feature is unconfigured or the call fails.
    [Function(nameof(RecommendTriageAction))]
    public async Task<IActionResult> Recommend(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mailbox/message/recommend")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        string? Opt(string name) { var v = request.Query[name].ToString(); return string.IsNullOrWhiteSpace(v) ? null : v; }
        var query = new RecommendTriageAction(id, Opt("imid"), Opt("cid"), Opt("subject"), Opt("from"), Opt("fromName"));
        return new OkObjectResult(await recommend.HandleAsync(query, request.HttpContext.RequestAborted));
    }

    [Function(nameof(DiscardMessage))]
    public async Task<IActionResult> Discard(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/discard")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var command = await ReadBody<DiscardMessage>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId))
            return new BadRequestObjectResult("messageId is required.");
        return new OkObjectResult(await discard.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(RestoreMessage))]
    public async Task<IActionResult> Restore(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/restore")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var command = await ReadBody<RestoreMessage>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId))
            return new BadRequestObjectResult("messageId is required.");
        return new OkObjectResult(await restore.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(RemoveTagFromMessage))]
    public async Task<IActionResult> RemoveTag(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/remove-tag")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var command = await ReadBody<RemoveTagFromMessage>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.Tag))
            return new BadRequestObjectResult("messageId and tag are required.");
        return new OkObjectResult(await removeTag.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(AssignMessageToRequest))]
    public async Task<IActionResult> Assign(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/assign")] HttpRequest request)
    {
        if (await Gate(request) is { } deny) return deny;
        var command = await ReadBody<AssignMessageToRequest>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.RequestId))
            return new BadRequestObjectResult("messageId and requestId are required.");
        return new OkObjectResult(await assign.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(CreateRequestFromMessage))]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-request")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var command = await ReadBody<CreateRequestFromMessage>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.ProjectId))
            return new BadRequestObjectResult("messageId and projectId are required.");

        // The raiser is always the signed-in triager.
        command = command with { RaisedByEmail = signedInUser.Email };
        return new OkObjectResult(await create.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    // Triage "Reply in thread": the reply written in the portal is staged as an Outlook draft on
    // the email (projects mailbox, thread quoted behind it) and a General request carrying the same
    // reply is created in the background — replying IS the triage. Nothing is sent; the pre-filled
    // draft is reviewed and sent from the mailbox itself.
    [Function(nameof(ReplyInThreadFromMessage))]
    public async Task<IActionResult> ReplyInThread(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/reply-in-thread")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();

        var command = await ReadBody<ReplyInThreadFromMessage>(request);
        if (command is null || string.IsNullOrWhiteSpace(command.MessageId) || string.IsNullOrWhiteSpace(command.ProjectId)
            || string.IsNullOrWhiteSpace(command.ReplyBody))
            return new BadRequestObjectResult("messageId, projectId and replyBody are required.");

        // The raiser is always the signed-in triager.
        command = command with { RaisedByEmail = signedInUser.Email };
        return new OkObjectResult(await replyInThread.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    // Returns a deny result (401/403) when the caller may not triage, or null when they may.
    private async Task<IActionResult?> Gate(HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new ForbidResult();
        return null;
    }

    private static (string? Cursor, int Take) Paging(HttpRequest request)
    {
        var cursor = request.Query["cursor"].ToString();
        var take = int.TryParse(request.Query["take"], out var t) ? t : 25;
        return (string.IsNullOrWhiteSpace(cursor) ? null : cursor, take);
    }

    private static async Task<T?> ReadBody<T>(HttpRequest request) where T : class
    {
        try { return await request.ReadFromJsonAsync<T>(); }
        catch { return null; }
    }
}
