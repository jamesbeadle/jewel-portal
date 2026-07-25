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

    public Task SendPasswordResetAsync(string email, string displayName, string resetLink, CancellationToken cancellationToken)
    {
        // A self-service reset has nobody to hand the link to, so without a mail provider the
        // request simply cannot complete. Say so loudly in the log rather than failing the request,
        // which would tell an anonymous caller whether the address exists.
        logger.LogWarning(
            "No email provider configured; the password reset for {Email} could not be delivered. " +
            "An administrator can send them a reset link from the Users panel instead.",
            email);
        return Task.CompletedTask;
    }
}
