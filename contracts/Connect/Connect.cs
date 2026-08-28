namespace Jewel.JPMS.Contracts.Connect;

/// <summary>What the consent page shows before the user approves: who is asking.</summary>
public sealed record ConnectClientInfo(string ClientId, string ClientName);

/// <summary>
/// The consent page's answer. Everything except <paramref name="Approved"/> is the authorise
/// request replayed verbatim — the server re-validates all of it against the database and the
/// signed-in session before minting anything.
/// </summary>
public sealed record ApproveConnectRequest(
    string ClientId,
    string RedirectUri,
    string State,
    string CodeChallenge,
    string Scope,
    string? Resource,
    bool Approved);

/// <summary>Where the consent page sends the browser next — back to the AI tool, with either a
/// code or an access_denied error in the query string.</summary>
public sealed record ApproveConnectResponse(string RedirectTo);

/// <summary>One connected AI tool on the profile page — a live refresh-token family.</summary>
public sealed record AiConnection(
    string FamilyId,
    string ClientName,
    /// <summary>Who approved it. Always the caller's own email unless an Admin is listing.</summary>
    string UserEmail,
    DateTimeOffset ConnectedAt,
    DateTimeOffset? LastUsedAt);

public sealed record RevokeAiConnectionRequest(string FamilyId);
