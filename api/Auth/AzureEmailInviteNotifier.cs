using Azure;
using Azure.Communication.Email;

namespace Jewel.JPMS.Api.Auth;

/// <summary>Sends invite and password-reset links through Azure Communication Services Email.</summary>
public sealed class AzureEmailInviteNotifier : IInviteNotifier
{
    private readonly EmailClient client;
    private readonly string senderAddress;

    public AzureEmailInviteNotifier(EmailClient client, string senderAddress)
    {
        this.client = client;
        this.senderAddress = senderAddress;
    }

    public async Task SendInviteAsync(string email, string displayName, string inviteLink, CancellationToken cancellationToken)
    {
        await client.SendAsync(
            WaitUntil.Started,
            senderAddress,
            email,
            InviteEmailBody.Subject,
            InviteEmailBody.Html(displayName, inviteLink),
            InviteEmailBody.PlainText(displayName, inviteLink),
            cancellationToken);
    }

    public async Task SendPasswordResetAsync(string email, string displayName, string resetLink, CancellationToken cancellationToken)
    {
        await client.SendAsync(
            WaitUntil.Started,
            senderAddress,
            email,
            PasswordResetEmailBody.Subject,
            PasswordResetEmailBody.Html(displayName, resetLink),
            PasswordResetEmailBody.PlainText(displayName, resetLink),
            cancellationToken);
    }
}
