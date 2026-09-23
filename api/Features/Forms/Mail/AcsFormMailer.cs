using Azure;
using Azure.Communication.Email;

namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>
/// Sends from the forms' sender (the portal's own unless one is configured) with the office
/// address as the Reply-To, because every form email says "reply to this email" when a link has
/// expired, and that reply must reach the office that sent it.
/// </summary>
public sealed class AcsFormMailer : IFormMailer
{
    private readonly EmailClient client;
    private readonly FormSiteOptions options;
    private readonly ILogger<AcsFormMailer> logger;

    public AcsFormMailer(EmailClient client, FormSiteOptions options, ILogger<AcsFormMailer> logger)
    {
        this.client = client;
        this.options = options;
        this.logger = logger;
    }

    public bool IsConfigured => true;

    public async Task SendAsync(FormEmail email, CancellationToken cancellationToken)
    {
        var recipients = new EmailRecipients(email.To.Select(address => new EmailAddress(address)).ToList());
        var message = new EmailMessage(options.Sender, recipients,
            new EmailContent(email.Subject) { Html = email.Html, PlainText = email.Text });
        message.ReplyTo.Add(new EmailAddress(JewelBespokeBuild.Email, JewelBespokeBuild.ShortName));
        try
        {
            await client.SendAsync(WaitUntil.Started, message, cancellationToken);
        }
        catch (RequestFailedException refusal)
        {
            logger.LogWarning(refusal, "A form email was refused by ACS: {Status}.", refusal.Status);
            throw new InvalidOperationException($"The email couldn't be sent ({refusal.Status}). {refusal.Message}");
        }
    }
}
