using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Auth;

/// <summary>
/// Fallback used when no Communication Services connection string is configured. The invite link is
/// still returned to the admin from the endpoint, so invites keep working; this records that the
/// email was not sent. The link itself is never logged because it grants access on its own.
/// </summary>
public sealed class LoggingInviteNotifier : IInviteNotifier
{
    private readonly ILogger<LoggingInviteNotifier> logger;

    public LoggingInviteNotifier(ILogger<LoggingInviteNotifier> logger)
    {
        this.logger = logger;
    }

    public Task SendInviteAsync(string email, string displayName, string inviteLink, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "No email provider configured; invite for {Email} was not emailed. Share the link from the admin screen.",
            email);
        return Task.CompletedTask;
    }
}
