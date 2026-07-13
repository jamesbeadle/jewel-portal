namespace Jewel.JPMS.Contracts.Subcontractors;

/// <summary>
/// Posted to /api/subcontractors/{subcontractorId}/portal-invite. Email and DisplayName are
/// optional overrides — when omitted the record's ContactEmail / ContactName are used. Returns an
/// Auth.InviteResult (the copyable set-password link).
/// </summary>
public sealed record InviteSubcontractorPortalUserRequest(string? Email = null, string? DisplayName = null);
