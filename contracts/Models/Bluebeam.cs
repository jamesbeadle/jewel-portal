namespace Jewel.JPMS.Models;

/// <summary>
/// The portal's shared Bluebeam Studio connection, as the admin page and the drawing pages see it.
/// IsConfigured says whether the app settings (client id/secret) are present at all; Connected
/// whether an admin has signed the shared Studio account in. No secrets ride on this — tokens stay
/// server-side. LastRefreshError is the admin page's "reconnect needed" signal: the nightly
/// keep-alive stamps it when the refresh token has died (Bluebeam kills them after 7 unused days).
/// </summary>
public sealed record BluebeamStatus(
    bool IsConfigured,
    bool Connected,
    string ConnectedEmail,
    DateTimeOffset? ConnectedAt,
    DateTimeOffset? LastRefreshSucceededAt,
    string? LastRefreshError);

/// <summary>Where the browser goes to grant the portal access — Bluebeam's consent page,
/// pre-wired with the app's client id, scopes and a signed state.</summary>
public sealed record BluebeamConnectStart(string AuthorizeUrl);
