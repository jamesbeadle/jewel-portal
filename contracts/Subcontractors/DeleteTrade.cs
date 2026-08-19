using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Subcontractors;

// Removes a trade from the curated master list (Admin → Trades). Refused (409, shown in the
// dialog) while any directory record still carries the trade — reassign those records first, so
// no company loses its trade silently. Bid packages hold the trade name as a snapshot string and
// never block deletion.
public sealed record DeleteTrade(string TradeId) : ICommand<Acknowledgement>;
