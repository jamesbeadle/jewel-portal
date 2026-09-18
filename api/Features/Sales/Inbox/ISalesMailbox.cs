using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

public interface ISalesMailbox
{
    bool IsConfigured { get; }
    string Address { get; }
    Task<MailboxPage> ListInboxAsync(string? cursor, int take, bool newestFirst, CancellationToken ct);
    Task<MailboxPage> SearchAsync(string query, int take, CancellationToken ct);
    Task<MailboxPage> ListConversationAsync(string conversationId, CancellationToken ct);
    Task<MailboxMessageDetail> GetDetailAsync(string messageId, CancellationToken ct);
    Task<MailboxSnapshot?> GetSnapshotAsync(string messageId, CancellationToken ct);
}

/// <summary>The window onto the sales mailbox, reading through the second set of mailbox-intake
/// Graph clients (<see cref="SalesMailboxGraph"/>). Replies leave by
/// <see cref="SalesOutboundEmail"/>, which shares the same clients.</summary>
public sealed class GraphSalesMailbox : ISalesMailbox
{
    private readonly MailboxGraphClient graph;
    private readonly GraphIntakeMessageReader reader;
    private readonly InboundEmailBodyBuilder bodies;
    private readonly string address;

    public GraphSalesMailbox(SalesMailboxGraph mailbox)
    {
        graph = mailbox.Client;
        reader = mailbox.Reader;
        bodies = mailbox.Bodies;
        address = mailbox.Address;
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

    private static Task<MailboxPage> Empty() => Task.FromResult(new MailboxPage(Array.Empty<MailboxMessage>(), null, 0));
}
