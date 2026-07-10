using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

/// <summary>
/// Removes a snapshot taken in error (with its lines). Never touches live report data. Any invoice
/// pointing at it has its snapshot link cleared.
/// </summary>
public sealed record DeleteValuationReportSnapshot(string ValuationReportSnapshotId) : ICommand<Acknowledgement>;
