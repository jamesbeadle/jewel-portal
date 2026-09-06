using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Sales;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

/// <summary>
/// The sales mailbox — sales@jewelbb.co.uk by default, app setting <c>SalesMailbox__Address</c>
/// to change it — read live through the SAME Graph app registration and credentials the
/// projects mailbox uses (MailboxIntake:TenantId / ClientId / ClientSecret), pointed at a second
/// mailbox. Nothing is tagged, moved or stored here: it is a window onto the Inbox, a thread
/// view, a reply, and a way to log an email on a lead. NOTE for the admin: the app registration
/// is restricted by an Exchange ApplicationAccessPolicy to the projects mailbox; the sales
/// mailbox must be added to that policy (docs/Requests-Mailbox-Setup-Checklist.md §3) or every
/// read here answers 403 — the page says so.
/// </summary>
public sealed class SalesMailboxOptions
{
    public string Address { get; set; } = "sales@jewelbb.co.uk";
    public bool Enabled { get; set; } = true;

    public static SalesMailboxOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new SalesMailboxOptions();
        var address = configuration["SalesMailbox:Address"];
        if (!string.IsNullOrWhiteSpace(address)) options.Address = address.Trim();
        if (bool.TryParse(configuration["SalesMailbox:Enabled"], out var enabled)) options.Enabled = enabled;
        return options;
    }
}

public interface ISalesMailbox
{
    bool IsConfigured { get; }
    string Address { get; }
    Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct);
    Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct);
    Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct);
    Task<MailboxMessageDetail> GetDetailAsync(string messageId, CancellationToken ct);
    Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, CancellationToken ct);
    /// <summary>Stage a reply-all draft with the body above the quoted history and send it. When
    /// the send is refused the draft stays in Drafts and the outcome says so.</summary>
    Task<SalesReplyOutcome> ReplyAsync(string messageId, string bodyHtml, CancellationToken ct);
}

/// <summary>Graph, over the mailbox-intake client classes instantiated a second time for the
/// sales address (their constructors are public and read the mailbox from the options they are
/// given, so no change to them was needed).</summary>
public sealed class GraphSalesMailbox : ISalesMailbox
{
    private readonly MailboxGraphClient graph;
    private readonly GraphIntakeMessageReader reader;
    private readonly InboundEmailBodyBuilder bodies;
    private readonly string address;

    public GraphSalesMailbox(MailboxGraphClient graph, GraphIntakeMessageReader reader, InboundEmailBodyBuilder bodies, string address)
    {
        this.graph = graph;
        this.reader = reader;
        this.bodies = bodies;
        this.address = address;
    }

    public bool IsConfigured => true;
    public string Address => address;

    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) =>
        graph.ListInboxAsync(cursor, take, newestFirst, ct);

    public Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct) =>
        graph.SearchAsync(query, take, ct);

    public Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct) =>
        graph.ListConversationAsync(conversationId, ct);

    public async Task<MailboxMessageDetail> GetDetailAsync(string messageId, CancellationToken ct)
    {
        var content = await reader.GetAsync(messageId, ct);
        if (content is null) return new MailboxMessageDetail(messageId, "", false, Array.Empty<IntakeAttachment>());
        var body = await bodies.BuildAsync(messageId, content, ct);
        var attachments = content.Attachments.Select(a => new IntakeAttachment(a.Name, a.Size, a.ContentType, a.Id)).ToList();
        return new MailboxMessageDetail(
            messageId, body, content.IsHtml, attachments,
            content.FromEmail, content.FromName, content.To, content.Cc, content.ReplyTo, content.Subject,
            MailboxAddress: address);
    }

    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, CancellationToken ct) =>
        graph.GetSnapshotAsync(messageId, null, ct);

    public async Task<SalesReplyOutcome> ReplyAsync(string messageId, string bodyHtml, CancellationToken ct)
    {
        var draft = await graph.CreateReplyDraftAsync(
            new MailboxReplyDraftMessage(messageId, bodyHtml, Array.Empty<MailboxDraftAttachment>()), ct);
        if (draft is null)
            return new SalesReplyOutcome(false, null, "The reply couldn't be staged in the sales mailbox — Graph refused. Check the mailbox access policy includes " + address + ".");
        if (await graph.SendDraftAsync(draft.Id, ct))
            return new SalesReplyOutcome(true, null, $"Sent from {address} to {string.Join(", ", draft.To)}.");
        return new SalesReplyOutcome(false, draft.WebLink,
            $"The reply is saved as a draft in {address} but couldn't be sent from here (Mail.Send not consented, or sending is switched off) — open it in Outlook to send.");
    }
}

/// <summary>Stands in when Graph isn't configured (or the sales inbox is switched off).</summary>
public sealed class NullSalesMailbox : ISalesMailbox
{
    public NullSalesMailbox(string address) { Address = address; }

    public bool IsConfigured => false;
    public string Address { get; }

    public Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct) => Empty();
    public Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct) => Empty();
    public Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct) => Empty();
    public Task<MailboxMessageDetail> GetDetailAsync(string messageId, CancellationToken ct) =>
        Task.FromResult(new MailboxMessageDetail(messageId, "", false, Array.Empty<IntakeAttachment>()));
    public Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, CancellationToken ct) => Task.FromResult<MailboxSnapshot?>(null);
    public Task<SalesReplyOutcome> ReplyAsync(string messageId, string bodyHtml, CancellationToken ct) =>
        Task.FromResult(new SalesReplyOutcome(false, null, "The sales mailbox isn't connected on the API (MailboxIntake credentials)."));

    private static Task<MailboxPage> Empty() => Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
}
