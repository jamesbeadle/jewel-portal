using System.Net;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;

namespace Jewel.JPMS.Api.Features.Sales.Imagine;

/// <summary>
/// The emails the post-identification journey sends, through Azure Communication Services like
/// the invite emails: to the prospect when their concepts are ready and when a proposal is sent
/// (both link back to their private imagine page), and to the sales mailbox when a prospect
/// submits, reacts or accepts — so the conversation continues from sales@ (the Reply-To on every
/// prospect email). Shared source: the worker sends "concepts ready", the api the rest.
/// </summary>
public interface IImagineNotifier
{
    bool IsConfigured { get; }
    Task SendConceptsReadyAsync(string toEmail, string toName, string link, int conceptCount, bool revision, CancellationToken ct);
    Task SendProposalAsync(string toEmail, string toName, string link, string title, string? note, CancellationToken ct);
    /// <summary>A note to the sales mailbox — a submission landed, a proposal was accepted.</summary>
    Task SendToSalesAsync(string subject, string html, string text, CancellationToken ct);
}

/// <summary>What the notifier needs to know that isn't the connection string.</summary>
public sealed class ImagineNotifierOptions
{
    public string SenderAddress { get; set; } = "DoNotReply@mail.jewelbb.co.uk";
    public string SalesAddress { get; set; } = "sales@jewelbb.co.uk";
    public string PublicSiteUrl { get; set; } = "https://portal.jewelbb.co.uk";

    public string ImagineLink(string token) => $"{PublicSiteUrl.TrimEnd('/')}/imagine/{token}";

    public static ImagineNotifierOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new ImagineNotifierOptions();
        var sender = configuration["InviteEmailSender"];
        if (!string.IsNullOrWhiteSpace(sender)) options.SenderAddress = sender;
        var sales = configuration["SalesMailbox:Address"];
        if (!string.IsNullOrWhiteSpace(sales)) options.SalesAddress = sales;
        var site = configuration["PublicSiteUrl"];
        if (!string.IsNullOrWhiteSpace(site)) options.PublicSiteUrl = site;
        return options;
    }
}

public sealed class AcsImagineNotifier : IImagineNotifier
{
    private readonly EmailClient client;
    private readonly ImagineNotifierOptions options;
    private readonly ILogger<AcsImagineNotifier> logger;

    public AcsImagineNotifier(EmailClient client, ImagineNotifierOptions options, ILogger<AcsImagineNotifier> logger)
    {
        this.client = client;
        this.options = options;
        this.logger = logger;
    }

    public bool IsConfigured => true;

    public Task SendConceptsReadyAsync(string toEmail, string toName, string link, int conceptCount, bool revision, CancellationToken ct)
    {
        var name = string.IsNullOrWhiteSpace(toName) ? "there" : toName.Trim();
        var subject = revision ? "Your revised concepts are ready" : "Your concepts from Jewel Bespoke Build";
        var lead = revision
            ? $"We've taken your notes and revised the concept — {conceptCount} new {(conceptCount == 1 ? "version is" : "versions are")} waiting for you."
            : $"We've looked at your photos and imagined {conceptCount} ways your home could change. Each one is rendered over your own photograph.";
        var html = Wrap(
            $"<p>Hello {WebUtility.HtmlEncode(name)},</p>"
            + $"<p>{WebUtility.HtmlEncode(lead)}</p>"
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\" style=\"display:inline-block;padding:12px 20px;background:#101111;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600\">See your concepts</a></p>"
            + "<p>Pick the one that speaks to you and tell us what you'd change — we'll revise it. Or simply reply to this email and we'll talk it through.</p>"
            + Signature());
        var text = $"Hello {name},\n\n{lead}\n\nSee your concepts: {link}\n\nPick the one that speaks to you and tell us what you'd change — we'll revise it. Or simply reply to this email and we'll talk it through.\n\nJewel Bespoke Build\n{options.SalesAddress}";
        return SendAsync(toEmail, subject, html, text, ct);
    }

    public Task SendProposalAsync(string toEmail, string toName, string link, string title, string? note, CancellationToken ct)
    {
        var name = string.IsNullOrWhiteSpace(toName) ? "there" : toName.Trim();
        var subject = $"Your proposal from Jewel Bespoke Build — {title}";
        var noteHtml = string.IsNullOrWhiteSpace(note) ? "" : $"<p>{WebUtility.HtmlEncode(note.Trim()).Replace("\n", "<br>")}</p>";
        var html = Wrap(
            $"<p>Hello {WebUtility.HtmlEncode(name)},</p>"
            + $"<p>Your proposal — <strong>{WebUtility.HtmlEncode(title)}</strong> — is ready. It sets out what we'd build, the price, the options you can add, and when the work would happen.</p>"
            + noteHtml
            + $"<p><a href=\"{WebUtility.HtmlEncode(link)}\" style=\"display:inline-block;padding:12px 20px;background:#101111;color:#ffffff;text-decoration:none;border-radius:6px;font-weight:600\">Open your proposal</a></p>"
            + "<p>Choose your options, read the terms, and accept when you're ready — or reply to this email with any question at all.</p>"
            + Signature());
        var text = $"Hello {name},\n\nYour proposal — {title} — is ready. It sets out what we'd build, the price, the options you can add, and when the work would happen.\n\n{(string.IsNullOrWhiteSpace(note) ? "" : note.Trim() + "\n\n")}Open your proposal: {link}\n\nChoose your options, read the terms, and accept when you're ready — or reply to this email with any question at all.\n\nJewel Bespoke Build\n{options.SalesAddress}";
        return SendAsync(toEmail, subject, html, text, ct);
    }

    public Task SendToSalesAsync(string subject, string html, string text, CancellationToken ct) =>
        SendAsync(options.SalesAddress, subject, Wrap(html), text, ct);

    private async Task SendAsync(string toEmail, string subject, string html, string text, CancellationToken ct)
    {
        var message = new EmailMessage(
            options.SenderAddress,
            new EmailRecipients(new[] { new EmailAddress(toEmail) }),
            new EmailContent(subject) { Html = html, PlainText = text });
        message.ReplyTo.Add(new EmailAddress(options.SalesAddress, "Jewel Bespoke Build"));
        try
        {
            await client.SendAsync(WaitUntil.Started, message, ct);
        }
        catch (RequestFailedException ex)
        {
            logger.LogWarning(ex, "Imagine email to {To} refused by ACS: {Status}.", toEmail, ex.Status);
            throw new InvalidOperationException($"The email couldn't be sent ({ex.Status}). {ex.Message}");
        }
    }

    private string Signature() =>
        $"<p style=\"color:#666\">Jewel Bespoke Build<br><a href=\"mailto:{options.SalesAddress}\">{options.SalesAddress}</a></p>";

    private static string Wrap(string body) =>
        "<div style=\"font-family:Poppins,Segoe UI,Helvetica,Arial,sans-serif;font-size:15px;line-height:1.55;color:#1a1a1a;max-width:600px\">"
        + body + "</div>";
}

/// <summary>No ACS connection: sends are refused with the reason so the caller can say so.</summary>
public sealed class NullImagineNotifier : IImagineNotifier
{
    private const string Reason = "Email isn't configured (CommunicationServicesConnectionString).";

    public bool IsConfigured => false;

    public Task SendConceptsReadyAsync(string toEmail, string toName, string link, int conceptCount, bool revision, CancellationToken ct) =>
        Task.FromException(new InvalidOperationException(Reason));

    public Task SendProposalAsync(string toEmail, string toName, string link, string title, string? note, CancellationToken ct) =>
        Task.FromException(new InvalidOperationException(Reason));

    public Task SendToSalesAsync(string subject, string html, string text, CancellationToken ct) =>
        Task.FromException(new InvalidOperationException(Reason));
}
