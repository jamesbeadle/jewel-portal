using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Auth;

/// <summary>Credentials posted to /api/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Posted to /api/auth/set-password to complete an invite or reset.</summary>
public sealed record SetPasswordRequest(string Token, string Password);

/// <summary>Posted by an admin to /api/auth/invite to create a user and mint an invite link.</summary>
public sealed record InviteUserRequest(string Email, string DisplayName, IReadOnlyList<Role> Roles);

/// <summary>Posted anonymously to /api/auth/forgot-password from the sign-in page.</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>Posted by an admin to /api/auth/send-reset to email an existing user a reset link.</summary>
public sealed record SendPasswordResetRequest(string Email);

/// <summary>The deliberately uninformative answer to a self-service reset request — the same
/// whether or not the address has an account, so the endpoint cannot be used to enumerate users.</summary>
public sealed record PasswordResetAcknowledgement(string Message);

/// <summary>The signed-in user, returned by /api/auth/me, /api/auth/login and set-password.
/// SubcontractorId is set only for portal-scoped subcontractor contacts.
/// Roles is the EFFECTIVE list (a directory Admin role expands to every role), so it cannot say
/// who the user really is — HomeRole is that answer: their first directory-assigned role that
/// isn't Administrator (HomeRoleSelection.From). RevertToOwnRole is the per-user opt-in
/// (administered on Admin → Users) for the "Viewing as" switch defaulting back to HomeRole
/// after two hours.</summary>
public sealed record AuthenticatedUserResponse(
    string Email, string DisplayName, IReadOnlyList<Role> Roles, string? SubcontractorId = null,
    Role? HomeRole = null, bool RevertToOwnRole = false);

/// <summary>Result of creating an invite or a reset — includes the copyable link for the admin to send.</summary>
public sealed record InviteResult(string Email, string DisplayName, string InviteLink, DateTimeOffset ExpiresAt);

/// <summary>Tells the set-password page whether a token is valid and who it belongs to.
/// IsReset distinguishes "you're resetting a password you already had" from a first-time invite, so
/// the page can greet the user correctly.</summary>
public sealed record InviteValidation(bool Valid, string? Email, string? DisplayName, bool IsReset = false);
