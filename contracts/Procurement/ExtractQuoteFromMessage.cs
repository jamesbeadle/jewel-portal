using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

// Ask Claude to read a tender-response email (tagged to the bid package) and propose the quote it
// contains: which subcontractor it came from and their price against each of the package's line
// items. This is a PROPOSAL — nothing is saved. The user reviews/edits the lines in the UI and then
// commits them with SaveExtractedQuote. Degrades gracefully: when no LLM is configured, Proposed is
// false and Lines carries one empty row per package line item for manual entry.
public sealed record ExtractQuoteFromMessage(
    string BidPackageId,
    string MessageId) : ICommand<QuoteExtractionProposal>;

// What came out of the email. SubcontractorId is matched from the sender's address against the
// package's recipients (null when no match — the user picks). Lines align to the package's line
// items via BidPackageLineItemId where the model could match them.
public sealed record QuoteExtractionProposal(
    bool Proposed,                       // true when the LLM produced this; false = manual skeleton
    string? SubcontractorId,
    string Notes,                        // exclusions/caveats the model spotted in the email
    IReadOnlyList<QuoteExtractionLine> Lines);

public sealed record QuoteExtractionLine(
    string? BidPackageLineItemId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal Total);
