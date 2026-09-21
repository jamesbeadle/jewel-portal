namespace Jewel.JPMS.Contracts.Architects;

/// <summary>
/// Posted to /api/architects/{architectId}/portal-invite. Email and DisplayName are optional
/// overrides — when omitted the practice's ContactEmail / ContactName are used. Returns an
/// Auth.InviteResult (the copyable set-password link).
/// </summary>
public sealed record InviteArchitectPortalUserRequest(string? Email = null, string? DisplayName = null);
