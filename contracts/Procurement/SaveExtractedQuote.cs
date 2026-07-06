using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Commit a reviewed tender submission: creates the Quote (Value = sum of line totals) and its
// per-line pricing, marks the subcontractor's recipient row Responded, and moves an Inviting
// package to QuotesReceived. Re-submitting for the same (package, subcontractor) replaces their
// previous quote and its lines — a subbie has one live submission per package. Returns the Quote.
public sealed record SaveExtractedQuote(
    string BidPackageId,
    string SubcontractorId,
    string Notes,
    IReadOnlyList<QuoteExtractionLine> Lines) : ICommand<Quote>;
