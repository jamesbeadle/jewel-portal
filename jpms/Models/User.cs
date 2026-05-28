namespace Jewel.JPMS.Models;

public sealed record AuthenticatedUser(
    string Email,
    string DisplayName,
    AuthProvider Provider);
