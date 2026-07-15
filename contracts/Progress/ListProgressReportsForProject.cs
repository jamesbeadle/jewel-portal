using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Progress;

/// <summary>Lists a project's progress reports (newest first), each with its selected update ids.</summary>
public sealed record ListProgressReportsForProject(string ProjectId)
    : IQuery<IReadOnlyList<ProgressReport>>;
