using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

/// <summary>Every project for an internal caller; for an architect login, the projects that
/// name their practice as the party (the endpoint fills <see cref="ArchitectId"/> from the
/// login itself — never from the request).</summary>
public sealed record ListProjectsVisibleToUser(string? ArchitectId = null) : IQuery<IReadOnlyList<Project>>;
