namespace Jewel.JPMS.Api.Auth;

/// <summary>Delivers single-use account links — the set-password link for a new invite, and the
/// reset link for somebody who already has an account.</summary>
public interface IInviteNotifier
{
    Task SendInviteAsync(string email, string displayName, string inviteLink, CancellationToken cancellationToken);

    Task SendPasswordResetAsync(string email, string displayName, string resetLink, CancellationToken cancellationToken);
}
