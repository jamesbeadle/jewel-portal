using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Lists a project's progress updates (newest first), each with its photos.</summary>
public sealed record ListProgressUpdatesForProject(string ProjectId)
    : IQuery<IReadOnlyList<ProgressUpdate>>;
