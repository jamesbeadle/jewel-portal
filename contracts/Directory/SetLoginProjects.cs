using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Directory;

/// <summary>POST /api/directory/projects — the projects an architect login may see, given in
/// Admin → Users (2026-09-25). A full-set write: the projects named ARE the login's projects, and
/// any grant not named is removed. GrantedByEmail is stamped server-side from the signed-in
/// administrator.</summary>
public sealed record SetLoginProjects(
    string Email,
    IReadOnlyList<string> ProjectIds,
    string GrantedByEmail = "") : ICommand<Acknowledgement>;
