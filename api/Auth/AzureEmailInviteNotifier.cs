using Azure;
using Azure.Communication.Email;

namespace Jewel.JPMS.Api.Auth;

/// <summary>Sends invite links through Azure Communication Services Email.</summary>
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
}
