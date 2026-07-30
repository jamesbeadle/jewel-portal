using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Directory;

/// <summary>Permanently deletes a REVOKED user's record — directory row, roles, credential,
/// outstanding links and sessions. Only available once the user has been revoked, so the
/// destructive step is always a second, deliberate act rather than a misclick on a live
/// account.</summary>
public sealed record DeleteDirectoryUser(string Email) : ICommand<Acknowledgement>;
