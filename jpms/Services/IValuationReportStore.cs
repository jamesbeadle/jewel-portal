
namespace Jewel.JPMS.Services;

public interface IValuationReportStore
{
    IReadOnlyList<ValuationLineItem> LinesFor(string projectId);
    IReadOnlyList<ValuationClaim> ClaimsFor(string projectId);
    IReadOnlyList<ClaimLine> EntriesFor(string claimId);

    /// <summary>True once this project's valuation lines AND claims have both landed. Every
    /// figure derived from the report (works complete, revised contract sum, retention) is a sum
    /// over those two lists, and each answers empty until it arrives — so a page that renders
    /// before this is true states a confident zero it will silently correct.</summary>
    bool ReportLoadedFor(string projectId);

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
    /// <summary>Bulk upsert of % complete across many lines of a Draft claim in one round trip —
    /// entering an opening position (mid-project join) or a heavy-update month.</summary>
    Task<IReadOnlyList<ClaimLine>> RecordEntriesAsync(string projectId, RecordClaimEntries command);
    Task<ValuationClaim> PreapproveClaimAsync(string projectId, string claimId);
    /// <summary>Undo an unintended preapproval: Preapproved → Draft, totals compute live again.</summary>
    Task<ValuationClaim> ReopenClaimAsync(string projectId, string claimId);
    Task<ValuationClaim> ConfirmClaimAsync(string projectId, string claimId);
    /// <summary>Sets the claim's period name ("June 2026"); allowed at any status. Empty clears it.</summary>
    Task<ValuationClaim> RenameClaimAsync(string projectId, string claimId, string name);
    /// <summary>Deletes a claim and its lines (refused while a live invoice stands against it).</summary>
    Task DeleteClaimAsync(string projectId, string claimId);

    // The valuation as a statement (2026-09-18: the locked claim IS the statement): a locked
    // claim's frozen rows, a Draft's working copy. Fetched on demand, never cached — the viewer
    // and the export read it fresh. A retired snapshot id resolves to its claim's statement.
    Task<ValuationStatement> GetStatementAsync(string claimId);

    event Action? OnChange;
}
