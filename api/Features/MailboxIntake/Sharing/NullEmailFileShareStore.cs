namespace Jewel.JPMS.Api.Features.MailboxIntake.Sharing;

/// <summary>
/// No-op share store for environments with no storage configured. <see cref="IsConfigured"/> is
/// false so callers keep their pre-link behaviour — attach everything (request drafts) or refuse
/// the oversized email outright (invite draft, compose) — instead of staging an email whose links
/// were never minted.
/// </summary>
public sealed class NullEmailFileShareStore : IEmailFileShareStore
{
    public bool IsConfigured => false;

    public Task<EmailFileShareLink?> ShareAsync(
        string scope, string fileName, string contentType, byte[] content, CancellationToken cancellationToken) =>
        Task.FromResult<EmailFileShareLink?>(null);
}
