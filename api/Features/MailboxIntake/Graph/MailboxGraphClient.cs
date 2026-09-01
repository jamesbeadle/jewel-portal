using System.Net;
using System.Net.Http.Headers;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;
/// <summary>Graph REST implementation (HttpClient + app-only token).</summary>
public sealed partial class MailboxGraphClient : IMailboxGraphClient
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private const string Summary =
        "id,internetMessageId,conversationId,subject,bodyPreview,from,receivedDateTime,hasAttachments,categories,isDraft";

    private readonly HttpClient _http;
    private readonly GraphTokenProvider _tokens;
    private readonly MailboxIntakeOptions _options;
    private readonly ILogger<MailboxGraphClient> _logger;

    public MailboxGraphClient(
        HttpClient http, GraphTokenProvider tokens, MailboxIntakeOptions options, ILogger<MailboxGraphClient> logger)
    {
        _http = http;
        _tokens = tokens;
        _options = options;
        _logger = logger;
    }

    private string Mailbox => Uri.EscapeDataString(_options.Mailbox);

    // The triage queue and discarded pile are Inbox views by definition — the mailbox's own sent
    // copies are never "to be triaged". The tag/conversation reads below span the WHOLE mailbox
    // (Sent Items included) so a reply sent from the project mailbox itself appears in a record's
    // correspondence: a sent message never arrives back in the Inbox, so an inbox-scoped read would
    // silently drop the outbound leg of every thread. Unsent drafts are excluded client-side.

}
