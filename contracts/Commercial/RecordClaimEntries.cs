using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// One line's cumulative % complete within a bulk update.
public sealed record ClaimEntryInput(
    string ValuationLineItemId,
    decimal PercentComplete);

// Upserts many lines' % complete on a Draft claim in one command — the bulk-edit mode
// behind entering an opening position (joining a project mid-way) or a heavy-update
// month, where per-cell entry across a 200+ line bill is impractical. Each entry gets
// the same cumulative/period-increment treatment as a single RecordClaimEntry.
public sealed record RecordClaimEntries(
    string ValuationClaimId,
    IReadOnlyList<ClaimEntryInput> Entries) : ICommand<IReadOnlyList<ClaimLine>>;
