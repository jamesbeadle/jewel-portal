using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>Snapshot headers for a project, newest first (lines are fetched per snapshot).</summary>
public sealed record ListValuationReportSnapshotsForProject(string ProjectId) : IQuery<IReadOnlyList<ValuationReportSnapshot>>;
