namespace Jewel.JPMS.Api.Features.RecordLinks.Providers;

// The ONE list a triager files a valuation email to (Nigel, 2026-09-15): per period, the frozen
// statement when one has been taken, otherwise the live claim — never both. Until this the
// Client pane offered "Valuation claims" AND "Valuation report snapshots" as two drawers, so a
// period with a statement out could be filed to any of three rows. The triager knows the report
// as a point in time; one row per point in time is the choice they can get right.
//
// The rule, newest period first:
//   - a claim with a LIVE (non-superseded) snapshot frozen from it is represented by that
//     snapshot — by each of them, newest first, when more than one stands (a raise and a
//     later submission on the same period);
//   - a claim with none is its own row: the live period (Draft / Preapproved), or a Confirmed
//     period that was never snapshotted — a late reply about June still belongs on June, and
//     the claim provider's IsActive=false plus its age put it at the bottom;
//   - superseded snapshots leave the picker altogether. Their mail still reads: the snapshot
//     viewer shows it, and the claim reads every statement frozen from it (the companion read
//     in RecordEmailReader);
//   - a live snapshot whose claim is missing (none in practice — every capture stamps the
//     latest claim) trails the list rather than vanishing.
// Each row keeps its OWN Type/RecordId, so the link and read paths still resolve through the
// owning provider — the SchedulingLinkProvider pattern (the bucket and the NOD/EOT/LAD rows in
// one picker). Pure over the two providers' rows, so the rule is unit-testable without a
// database or a mailbox (ValuationReportLinkTargetsTests).
public static class ValuationReportLinkTargets
{
    public static IReadOnlyList<LinkableRecord> Merge(
        IReadOnlyList<LinkableRecord> claims,                        // ValuationClaimLinkProvider rows, newest first
        IReadOnlyList<LinkableRecord> snapshots,                     // ValuationReportSnapshotLinkProvider rows, newest first; IsActive = not superseded
        IReadOnlyDictionary<string, string?> claimIdBySnapshotId)    // the claim each snapshot was frozen from
    {
        var liveByClaim = new Dictionary<string, List<LinkableRecord>>(StringComparer.Ordinal);
        var claimless = new List<LinkableRecord>();
        foreach (var snapshot in snapshots)
        {
            if (!snapshot.IsActive) continue; // superseded — a statement the client never kept
            var claimId = claimIdBySnapshotId.TryGetValue(snapshot.RecordId, out var id) ? id : null;
            if (string.IsNullOrEmpty(claimId)) { claimless.Add(snapshot); continue; }
            if (!liveByClaim.TryGetValue(claimId, out var rows))
                liveByClaim[claimId] = rows = new List<LinkableRecord>();
            rows.Add(snapshot);
        }

        var merged = new List<LinkableRecord>(claims.Count + claimless.Count);
        foreach (var claim in claims)
        {
            if (liveByClaim.Remove(claim.RecordId, out var statements)) merged.AddRange(statements);
            else merged.Add(claim);
        }
        // Statements whose claim is no longer listed, then the claim-less — never dropped.
        foreach (var strays in liveByClaim.Values) merged.AddRange(strays);
        merged.AddRange(claimless);
        return merged.AsReadOnly();
    }
}
