namespace Jewel.JPMS.Api.Features.Bluebeam;

/// <summary>
/// Stands in when the Bluebeam app settings are absent (local dev, or before setup). Reads of the
/// status degrade gracefully via IsConfigured; anything that actually needs Bluebeam fails with
/// the message that names the settings to add.
/// </summary>
public sealed class NullBluebeamClient : IBluebeamClient
{
    private const string NotConfiguredMessage =
        "Bluebeam isn't configured — add the Bluebeam__ClientId and Bluebeam__ClientSecret app settings.";

    public bool IsConfigured => false;

    public Task<BluebeamTokens> ExchangeCodeAsync(string code, CancellationToken cancellationToken) => Fail<BluebeamTokens>();
    public Task<BluebeamTokens> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => Fail<BluebeamTokens>();
    public Task<BluebeamUser> GetCurrentUserAsync(string accessToken, CancellationToken cancellationToken) => Fail<BluebeamUser>();
    public Task<string> CreateSessionAsync(string accessToken, string sessionName, CancellationToken cancellationToken) => Fail<string>();
    public Task<BluebeamFileSlot> AddSessionFileAsync(string accessToken, string sessionId, string fileName, long fileSizeBytes, CancellationToken cancellationToken) => Fail<BluebeamFileSlot>();
    public Task UploadFileBytesAsync(BluebeamFileSlot slot, byte[] pdfBytes, CancellationToken cancellationToken) => Fail<object>();
    public Task ConfirmUploadAsync(string accessToken, string sessionId, string fileId, CancellationToken cancellationToken) => Fail<object>();
    public Task<string> GetMarkupsRawJsonAsync(string accessToken, string sessionId, string fileId, CancellationToken cancellationToken) => Fail<string>();
    public Task FinalizeSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken) => Fail<object>();
    public Task DeleteSessionAsync(string accessToken, string sessionId, CancellationToken cancellationToken) => Fail<object>();

    private static Task<T> Fail<T>() => Task.FromException<T>(new InvalidOperationException(NotConfiguredMessage));
}
