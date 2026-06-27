using Azure.Core;
using Azure.Identity;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>
/// Acquires and caches an app-only Microsoft Graph access token using the app registration's
/// client secret (client-credentials flow). Mirrors the worker's provider — the API needs its own
/// because it reads message bodies on demand when a triager opens an email. The token is cached
/// in-process and refreshed a few minutes before expiry, so the hot path makes no token call.
/// </summary>
public sealed class GraphTokenProvider
{
    private static readonly string[] Scopes = { "https://graph.microsoft.com/.default" };

    private readonly ClientSecretCredential _credential;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AccessToken _cached;

    public GraphTokenProvider(MailboxIntakeOptions options)
    {
        _credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
    }

    public async ValueTask<string> GetTokenAsync(CancellationToken ct)
    {
        if (IsFresh(_cached))
            return _cached.Token;

        await _gate.WaitAsync(ct);
        try
        {
            if (IsFresh(_cached))
                return _cached.Token;

            _cached = await _credential.GetTokenAsync(new TokenRequestContext(Scopes), ct);
            return _cached.Token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool IsFresh(AccessToken token) =>
        token.Token is not null && token.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5);
}
