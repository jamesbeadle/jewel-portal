using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Leads;

public sealed record RecordBidDecision(
    string LeadId,
    bool ShouldBid,
    string Reason,
    string DecidedByEmail) : ICommand<BidDecision>;
