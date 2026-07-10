using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>One snapshot with its frozen lines — feeds the read-only snapshot viewer.</summary>
public sealed record GetValuationReportSnapshot(string ValuationReportSnapshotId) : IQuery<ValuationReportSnapshotDetail>;
