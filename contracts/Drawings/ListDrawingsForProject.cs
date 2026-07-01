using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

/// <summary>
/// Lists a project's drawings, each carrying its latest approved revision label and status counts.
/// When <see cref="ApprovedOnly"/> is true, only drawings that have an approved revision are returned.
/// </summary>
public sealed record ListDrawingsForProject(
    string ProjectId,
    bool ApprovedOnly = false) : IQuery<IReadOnlyList<Drawing>>;
