using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Auth;

/// <summary>Credentials posted to /api/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Posted to /api/auth/set-password to complete an invite or reset.</summary>
public sealed record SetPasswordRequest(string Token, string Password);

/// <summary>Posted by an admin to /api/auth/invite to create a user and mint an invite link.</summary>
public sealed record InviteUserRequest(string Email, string DisplayName, IReadOnlyList<Role> Roles);

/// <summary>The signed-in user, returned by /api/auth/me, /api/auth/login and set-password.</summary>
public sealed record AuthenticatedUserResponse(string Email, string DisplayName, IReadOnlyList<Role> Roles);

/// <summary>Result of creating an invite — includes the copyable link for the admin to send.</summary>
public sealed record InviteResult(string Email, string DisplayName, string InviteLink, DateTimeOffset ExpiresAt);

/// <summary>Tells the set-password page whether a token is valid and who it belongs to.</summary>
public sealed record InviteValidation(bool Valid, string? Email, string? DisplayName);
