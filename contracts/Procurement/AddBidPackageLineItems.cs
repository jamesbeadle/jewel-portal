using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Append line items to a bid package without touching the existing set. Unlike
// SetBidPackageLineItems (wholesale replace — existing rows are deleted and recreated with new
// ids, dropping coverage links), this preserves existing rows exactly: ids, coverage and any
// quote-line references stay intact. Used by the AI-draft review flow, where "accepted lines are
// added and nothing else changes" is the promise made to the user. Returns the full stored set.
public sealed record AddBidPackageLineItems(
    string BidPackageId,
    IReadOnlyList<BidPackageLineItemInput> LineItems) : ICommand<IReadOnlyList<BidPackageLineItem>>;
