using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Renames a trade on the curated master list (Admin → Trades). The new name is normalised like
// AddTrade's (trimmed, first letter capitalised); renaming to a name another trade already holds
// is refused rather than minting a duplicate. Directory records reference trades by id, so every
// record carrying the trade shows the new name at once. Bid packages keep the trade NAME they were
// created with (a snapshot string), so historical packages are unaffected.
public sealed record RenameTrade(string TradeId, string Name) : ICommand<Trade>;
