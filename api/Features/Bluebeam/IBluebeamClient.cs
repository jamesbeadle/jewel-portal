namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// The outbound Bluebeam Studio API surface the portal uses — OAuth token exchange plus the
/// session dance an extraction runs (create session → add file → PUT bytes to the returned AWS
/// upload URL → confirm → read markups → finalise → delete). Every call is made in the connected
/// account's context; access tokens come from BluebeamTokenService, never from here. Failures
/// throw BluebeamCallFailedException with a message safe to store on the extraction row.
/// </summary>
public interface IBluebeamClient
{
    bool IsConfigured { get; }

    /// <summary>Exchanges the consent redirect's one-shot code for tokens (connect flow only).</summary>
    Task<BluebeamTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken);

    /// <summary>Trades a refresh token for fresh tokens. Bluebeam rotates the refresh token —
    /// the caller must persist the returned one or the connection dies.</summary>
    Task<BluebeamTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>The connected account's identity, for the admin page's "connected as …".</summary>
    Task<BluebeamUser> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>Creates a Studio session and returns its id.</summary>
    Task<string> CreateSessionAsync(string accessToken, string sessionName, CancellationToken cancellationToken);

    /// <summary>Registers a file slot on a session — the returned upload URL is only valid for
    /// ten minutes, so the PUT must follow immediately.</summary>
    Task<BluebeamFileSlot> AddSessionFileAsync(
        string accessToken, string sessionId, string fileName, long fileSizeBytes, CancellationToken cancellationToken);

    /// <summary>Raw PUT of the PDF bytes to the slot's AWS upload URL (no bearer — the URL is
    /// itself the credential).</summary>
    Task UploadFileBytesAsync(BluebeamFileSlot slot, byte[] pdfBytes, CancellationToken cancellationToken);

    /// <summary>Tells Bluebeam the upload finished, which makes the file live in the session.</summary>
    Task ConfirmUploadAsync(string accessToken, string sessionId, string fileId, CancellationToken cancellationToken);

    /// <summary>The file's markups as Bluebeam returned them — verbatim JSON, parsed elsewhere so
    /// a field the parser doesn't know is never lost.</summary>
    Task<string> GetMarkupsRawJsonAsync(
        string accessToken, string sessionId, string fileId, CancellationToken cancellationToken);

    /// <summary>Moves the session to Finalizing — Bluebeam's precondition for deleting it.</summary>
    Task FinalizeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken);

    /// <summary>Deletes a session. Extractions are stateless — the session only ever exists for
    /// the minutes one run needs it.</summary>
    Task DeleteSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken);
}

/// <summary>A token grant. ExpiresInSeconds counts from receipt; Bluebeam's access tokens live an
/// hour and its refresh tokens rotate on every use.</summary>
public sealed record BluebeamTokens(string AccessToken, string RefreshToken, int ExpiresInSeconds);

public sealed record BluebeamUser(string Email, string DisplayName);

/// <summary>A session file slot: Bluebeam's id for the file, the short-lived AWS URL the bytes go
/// to, and the exact Content-Type the PUT must carry (the docs are explicit that the upload's
/// Content-Type header must be the returned UploadContentType, or S3 refuses the signature).</summary>
public sealed record BluebeamFileSlot(string FileId, string UploadUrl, string UploadContentType);

/// <summary>A Bluebeam call failed with a message safe to surface (status + trimmed body).</summary>
public sealed class BluebeamCallFailedException : Exception
{
    public BluebeamCallFailedException(string message) : base(message) { }
}
