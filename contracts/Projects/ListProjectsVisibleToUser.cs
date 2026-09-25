using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

/// <summary>Every project for an internal caller; for an architect login, the projects given to
/// that login in Admin → Users; for a client login, its client's projects. The endpoint fills
/// <see cref="ArchitectLogin"/> and <see cref="ClientId"/> from the login itself — never from the
/// request.</summary>
public sealed record ListProjectsVisibleToUser(string? ArchitectLogin = null, string? ClientId = null) : IQuery<IReadOnlyList<Project>>;
