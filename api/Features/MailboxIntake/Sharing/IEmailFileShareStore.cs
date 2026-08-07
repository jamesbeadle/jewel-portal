namespace Jewel.JPMS.Api.Features.MailboxIntake.Sharing;

/// <summary>
/// Turns a file that is too large to attach to an email into a time-limited download link: the
/// bytes are COPIED into a dedicated private container (never linked in place — expiry and cleanup
/// must not touch the source drawing, and a drawing revised later must not change what a
/// subcontractor was sent) and a read-only SAS URL valid for <see cref="AzureBlobEmailFileShareStore.LinkLifetime"/>
/// is returned for the email body. Works with the storage account's public blob access disabled —
/// the SAS token is the whole grant. Callers decide WHICH files become links
/// (<see cref="EmailAttachmentPlanner"/>); this store only mints them.
/// </summary>
public interface IEmailFileShareStore
{
    /// <summary>False for the null store — callers keep their pre-link behaviour (attach or refuse)
    /// rather than staging an email that promises links it cannot mint.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Copies the file into the share container under the given scope (a record reference like
    /// "BPI-0001" — used only to keep the container tidy) and returns its download link, or null
    /// when a link cannot be minted (store unconfigured, or the credential cannot sign SAS URLs).
    /// Storage failures throw — a half-shared email must fail loudly, not send with dead links.
    /// </summary>
    Task<EmailFileShareLink?> ShareAsync(
        string scope, string fileName, string contentType, byte[] content, CancellationToken cancellationToken);
}

/// <summary>A minted download link: the file it serves, its size (for the email body), the SAS URL
/// and when the link stops working.</summary>
public sealed record EmailFileShareLink(string FileName, long SizeBytes, Uri Url, DateTimeOffset ExpiresAt);
