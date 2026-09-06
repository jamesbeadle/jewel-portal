using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

/// <summary>
/// The sales inbox's HTTP surface. Reads are SalesRoles.Readers; the reply and the log are the
/// sales team's. Message ids travel in the query string / body, never the route (Graph ids carry
/// characters a route won't).
/// </summary>
public sealed class SalesInboxEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly IQueryHandler<ListSalesInbox, SalesInboxPage> list;
    private readonly IQueryHandler<GetSalesInboxConversation, MailboxPage> conversation;
    private readonly IQueryHandler<GetSalesInboxMessage, MailboxMessageDetail> detail;
    private readonly ReplyToSalesEmailAuthorisation replyAuthorisation;
    private readonly ReplyToSalesEmailValidation replyValidation;
    private readonly ICommandHandler<ReplyToSalesEmail, SalesReplyOutcome> reply;
    private readonly LogSalesEmailToLeadAuthorisation logAuthorisation;
    private readonly LogSalesEmailToLeadValidation logValidation;
    private readonly ICommandHandler<LogSalesEmailToLead, LeadActivity> log;

    public SalesInboxEndpoints(
        SignedInUserResolver users,
        AuditActor auditActor,
        IQueryHandler<ListSalesInbox, SalesInboxPage> list,
        IQueryHandler<GetSalesInboxConversation, MailboxPage> conversation,
        IQueryHandler<GetSalesInboxMessage, MailboxMessageDetail> detail,
        ReplyToSalesEmailAuthorisation replyAuthorisation,
        ReplyToSalesEmailValidation replyValidation,
        ICommandHandler<ReplyToSalesEmail, SalesReplyOutcome> reply,
        LogSalesEmailToLeadAuthorisation logAuthorisation,
        LogSalesEmailToLeadValidation logValidation,
        ICommandHandler<LogSalesEmailToLead, LeadActivity> log)
    {
        this.users = users; this.auditActor = auditActor; this.list = list; this.conversation = conversation; this.detail = detail;
        this.replyAuthorisation = replyAuthorisation; this.replyValidation = replyValidation; this.reply = reply;
        this.logAuthorisation = logAuthorisation; this.logValidation = logValidation; this.log = log;
    }

    [Function(nameof(ListSalesInbox))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/inbox")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var cursor = request.Query["cursor"].ToString();
        var take = int.TryParse(request.Query["take"], out var t) ? t : 25;
        var newestFirst = !bool.TryParse(request.Query["newestFirst"], out var n) || n;
        var search = request.Query["search"].ToString();
        var query = new ListSalesInbox(string.IsNullOrWhiteSpace(cursor) ? null : cursor, take, newestFirst, string.IsNullOrWhiteSpace(search) ? null : search);
        return new OkObjectResult(await list.HandleAsync(query, request.HttpContext.RequestAborted));
    }

    [Function(nameof(GetSalesInboxConversation))]
    public async Task<IActionResult> Conversation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/inbox/conversation")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        return new OkObjectResult(await conversation.HandleAsync(new GetSalesInboxConversation(id), request.HttpContext.RequestAborted));
    }

    [Function(nameof(GetSalesInboxMessage))]
    public async Task<IActionResult> Detail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/inbox/message")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var id = request.Query["id"].ToString();
        if (string.IsNullOrWhiteSpace(id)) return new BadRequestObjectResult("id is required.");
        return new OkObjectResult(await detail.HandleAsync(new GetSalesInboxMessage(id), request.HttpContext.RequestAborted));
    }

    [Function(nameof(ReplyToSalesEmail))]
    public async Task<IActionResult> Reply(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/inbox/reply")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<ReplyToSalesEmail>();
        if (posted is null) return new BadRequestResult();
        var command = posted with { SentByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!replyAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = replyValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => reply.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(LogSalesEmailToLead))]
    public async Task<IActionResult> Log(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/inbox/log")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<LogSalesEmailToLead>();
        if (posted is null) return new BadRequestResult();
        var command = posted with { RecordedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!logAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = logValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => log.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    private static async Task<IActionResult> Run<T>(Func<Task<T>> handle)
    {
        try { return new OkObjectResult(await handle()); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
