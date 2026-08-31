namespace Jewel.JPMS.Contracts.Clients;

/// <summary>
/// Posted to /api/clients/{clientId}/portal-invite. Email and DisplayName are optional
/// overrides — when omitted the account's PrimaryContactEmail / PrimaryContactName are used.
/// Returns an Auth.InviteResult (the copyable set-password link).
/// </summary>
public sealed record InviteClientPortalUserRequest(string? Email = null, string? DisplayName = null);
