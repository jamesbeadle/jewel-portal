using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IValuationReportStore
{
    IReadOnlyList<ValuationLineItem> LinesFor(string projectId);
    IReadOnlyList<ValuationClaim> ClaimsFor(string projectId);
    IReadOnlyList<ClaimLine> EntriesFor(string claimId);

    Task<ValuationLineItem> AddLineAsync(AddValuationLineItem command);
    Task<ValuationLineItem> UpdateLineAsync(UpdateValuationLineItem command);
    Task RemoveLineAsync(string projectId, string lineItemId);

    Task<ValuationClaim> StartClaimAsync(StartValuationClaim command);
    Task<ClaimLine> RecordEntryAsync(string projectId, RecordClaimEntry command);
    Task<ValuationClaim> PreapproveClaimAsync(string projectId, string claimId);
    Task<ValuationClaim> ConfirmClaimAsync(string projectId, string claimId);

    event Action? OnChange;
}
