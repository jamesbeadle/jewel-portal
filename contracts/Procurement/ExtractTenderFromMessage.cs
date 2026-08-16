using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Procurement;

// Reads a subcontractor's tender email (body + any returned pricing-schedule spreadsheet, extracted
// server-side) and proposes the submission with Claude: each priced line mapped to the package's
// line items, the subcontractor identified from the sender, and every gap named. NOTHING is saved —
// the proposal pre-fills the Tender submission modal for the user to review, correct and commit
// through SaveExtractedQuote exactly as a manually keyed tender would be.
public sealed record ExtractTenderFromMessage(
    string BidPackageId,
    string MessageId) : ICommand<TenderExtraction>;

// The reviewable proposal. Proposed is false when the AI could not read the tender at all
// (unconfigured, unreachable, or an unparseable answer) — the modal then falls back to the blank
// package schedule, with Issues saying why. Issues is every gap found — package lines with no
// price, totals that don't reconcile, an unreadable attachment — merged from the model's own
// findings and the server's deterministic checks; empty Issues + a matched subcontractor is what
// Complete means, and an incomplete extraction is what "Draft supplier reply" answers.
public sealed record TenderExtraction(
    bool Proposed,
    string? SubcontractorId,
    string SubcontractorNote,
    string Notes,
    IReadOnlyList<QuoteExtractionLine> Lines,
    IReadOnlyList<string> Issues,
    bool Complete);
