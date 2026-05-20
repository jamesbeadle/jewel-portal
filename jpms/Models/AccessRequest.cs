namespace Jewel.JPMS.Models;

/// <summary>
/// A user who has signed in successfully but isn't on the approved directory yet
/// and has clicked "Request access". Held in the access-request store until an
/// administrator approves or declines.
/// </summary>
public sealed record AccessRequest(
    string Email,
    string DisplayName,
    AuthProvider Provider,
    DateTimeOffset RequestedAt
);
