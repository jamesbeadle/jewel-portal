using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IValuationReportStore
{
    IReadOnlyList<ValuationLineItem> LinesFor(string projectId);
    IReadOnlyList<ValuationClaim> ClaimsFor(string projectId);
    IReadOnlyList<ClaimLine> EntriesFor(string claimId);

    /// <summary>Starts a background refetch of the project's lines and claims even if cached,
    /// and marks per-claim entries stale so the next read refetches them. Call on page entry
    /// so navigating back to the Valuation tab shows fresh data (stale-while-revalidate).</summary>
    void Refresh(string projectId);

    Task<ValuationLineItem> AddLineAsync(AddValuationLineItem command);
    Task<ValuationLineItem> UpdateLineAsync(UpdateValuationLineItem command);
    Task RemoveLineAsync(string projectId, string lineItemId);

    Task<ValuationClaim> StartClaimAsync(StartValuationClaim command);
    Task<ClaimLine> RecordEntryAsync(string projectId, RecordClaimEntry command);
    Task<ValuationClaim> PreapproveClaimAsync(string projectId, string claimId);
    Task<ValuationClaim> ConfirmClaimAsync(string projectId, string claimId);

    // Immutable report snapshots — frozen copies behind invoice submissions plus
    // on-demand period-end records. Headers are cached per project (Refresh
    // revalidates); the full line-level detail is fetched per snapshot on demand.
    IReadOnlyList<ValuationReportSnapshot> SnapshotsFor(string projectId);
    Task<ValuationReportSnapshot> TakeSnapshotAsync(string projectId, string label);
    Task<ValuationReportSnapshotDetail> GetSnapshotAsync(string snapshotId);
    Task DeleteSnapshotAsync(string projectId, string snapshotId);

    event Action? OnChange;
}
