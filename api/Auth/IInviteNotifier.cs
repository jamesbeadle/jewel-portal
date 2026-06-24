namespace Jewel.JPMS.Api.Auth;

/// <summary>Delivers the single-use set-password link to a newly invited user.</summary>
public interface IInviteNotifier
{
    Task SendInviteAsync(string email, string displayName, string inviteLink, CancellationToken cancellationToken);
}
