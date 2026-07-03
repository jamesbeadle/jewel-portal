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

    event Action? OnChange;
}
