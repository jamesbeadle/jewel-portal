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
    /// <summary>Recodes the cost centre any line's value sits against (Admin/MD/FD/PM only); for a
    /// variation line the mirrored VO and its committed budget move with it, other lines just take
    /// the new code.</summary>
    Task<ValuationLineItem> SetLineCostCentreAsync(SetValuationLineCostCentre command);
    Task RemoveLineAsync(string projectId, string lineItemId);

    Task<ValuationClaim> StartClaimAsync(StartValuationClaim command);
    Task<ClaimLine> RecordEntryAsync(string projectId, RecordClaimEntry command);
    Task<ValuationClaim> PreapproveClaimAsync(string projectId, string claimId);
    /// <summary>Undo an unintended preapproval: Preapproved → Draft, totals compute live again.</summary>
    Task<ValuationClaim> ReopenClaimAsync(string projectId, string claimId);
    Task<ValuationClaim> ConfirmClaimAsync(string projectId, string claimId);
    /// <summary>Sets the claim's period name ("June 2026"); allowed at any status. Empty clears it.</summary>
    Task<ValuationClaim> RenameClaimAsync(string projectId, string claimId, string name);
    /// <summary>Deletes a claim and its entries; linked invoices/snapshots survive with the link cleared.</summary>
    Task DeleteClaimAsync(string projectId, string claimId);

    // Immutable report snapshots — frozen copies behind invoice submissions plus
    // on-demand period-end records. Headers are cached per project (Refresh
    // revalidates); the full line-level detail is fetched per snapshot on demand.
    IReadOnlyList<ValuationReportSnapshot> SnapshotsFor(string projectId);
    Task<ValuationReportSnapshot> TakeSnapshotAsync(string projectId, string label);
    Task<ValuationReportSnapshotDetail> GetSnapshotAsync(string snapshotId);
    Task DeleteSnapshotAsync(string projectId, string snapshotId);

    event Action? OnChange;
}
