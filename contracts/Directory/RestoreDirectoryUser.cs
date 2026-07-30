using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Directory;

/// <summary>Reinstates a revoked user: their directory record, the roles they held at revocation,
/// and their ability to sign in (with their existing password, or a fresh invite if they never
/// set one).</summary>
public sealed record RestoreDirectoryUser(string Email) : ICommand<Acknowledgement>;
