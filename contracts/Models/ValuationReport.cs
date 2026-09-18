using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Models;

// Where a priced line sits in the bill. Mirrors the three blocks of the By France
// workbook (Contract Sum, PC Sums, Contingency) plus the variations register.
public enum ValuationElementType
{
    ContractWorks = 0,
    PcSum = 1,
    Contingency = 2,
    Variation = 3
}

// The nature of a single priced row.
public enum ValuationLineType
{
    Priced = 0,           // ordinary measured work
    ProvisionalSum = 1,   // PS / provisional allowance
    Omit = 2,             // negative line that removes scoped work (often a PS being replaced)
    Declined = 3,         // recorded but not priced into the total
    Tbc = 4               // to be confirmed; not priced into the total
}

// Claim lifecycle: a Draft is editable; Preapproved means "we are claiming this"
// (amounts locked, awaiting the client); Confirmed means the client has paid and the
// per-row claimed amounts are final, advancing CertifiedToDate for the next claim.
public enum ValuationClaimStatus
{
    Draft = 0,
    Preapproved = 1,
    Confirmed = 2
}

public static class ValuationClaimStatusExtensions
{
    // The one shared status wording (same arrangement as VariationOrderStatusExtensions): the
    // reader's word for a Preapproved claim is "Issued" — it has been locked and put to the
    // client for a decision (accountant 2026-08-26). The enum member keeps its name: it is a
    // persisted identifier (stored as an int, threaded through API routes), never UI copy.
    public static string DisplayName(this ValuationClaimStatus status) => status switch
    {
        ValuationClaimStatus.Draft => "Draft",
        ValuationClaimStatus.Preapproved => "Issued",
        ValuationClaimStatus.Confirmed => "Confirmed",
        _ => status.ToString()
    };
}

// One priced row of the bill — a contract/PC/contingency line, or a variation line.
// Entered manually through the UI; no BoQ seeding.
public sealed record ValuationLineItem(
    string ValuationLineItemId,
    string ProjectId,
    ValuationElementType ElementType,
    string SectionCode,        // works/PC, e.g. "A10"
    string SectionName,        // e.g. "Preliminaries"
    string VariationRef,       // variations, e.g. "V18"
    string VariationTitle,
    ValuationLineType LineType,
    string CostCode,           // e.g. "0001"
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal LineAmount,        // qty x rate; negative for omits
    string Comments,
    int DisplayOrder,
    // The client's schedule-of-works item number for THIS line ("1.03") — the ref the client
    // reconciles against, line by line. Beats the per-cost-centre ClientCostReferences map at
    // snapshot capture; empty falls back to the map. Trailing default keeps the positional
    // constructor stable for older callers.
    string ClientReference = "") : IVariationBillLine
{
    // Declined / TBC lines are recorded but never priced into any total.
    public bool CountsTowardTotals => LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc);
}

// One valuation period — the "Claim n" funds-request event — with frozen totals.
public sealed record ValuationClaim(
    string ValuationClaimId,
    string ProjectId,
    int ClaimNumber,
    DateTimeOffset ClaimDate,
    ValuationClaimStatus Status,
    decimal RetentionPercent,
    decimal RetentionReleasePercent,
    DateTimeOffset? PreapprovedAt,
    DateTimeOffset? ConfirmedAt,
    // Totals frozen when the claim is Confirmed:
    decimal ContractSum,
    decimal NetVariations,
    decimal RevisedContractSum,
    decimal TotalWorksComplete,
    decimal RetentionHeld,
    decimal RetentionReleased,
    decimal CertifiedToDate,
    decimal PaymentDueExVat,
    // Free-text period name (e.g. "June 2026"); renameable at any status. Empty for
    // claims from before names existed — display falls back to "Claim n".
    string Name = "",
    // Cash-up-front deposit: the % and opening balance stamped from the project's terms
    // when the claim starts (kept live on Drafts when terms change). DepositReleased is
    // the amount actually DEDUCTED from this claim's payment due — the deposit release
    // earned to date less the opening balance settled before tracking — frozen with the
    // other totals when the claim locks. Trailing defaults keep the positional
    // constructor stable for pre-deposit callers.
    decimal DepositPercent = 0m,
    decimal DepositReleased = 0m,
    decimal DepositReleasedOpening = 0m,
    // When the valuation's lines were frozen onto its own rows — the moment "We're claiming
    // this" locked it (2026-09-18: the lock IS the statement; there is no separate snapshot).
    // Null while Draft, when the statement is a working copy computed from the live bill.
    DateTimeOffset? LockedAt = null)
{
    // "June 2026" when named, otherwise "Claim 3" — one rule for every claim label.
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? $"Claim {ClaimNumber}" : Name;

    // A locked valuation (Preapproved or Confirmed) carries its own frozen statement lines;
    // a Draft's statement is computed from the live bill each time it is read.
    public bool IsLocked => Status != ValuationClaimStatus.Draft;
}

// Per claim, per line item: the cumulative % complete entered and the resulting amounts.
public sealed record ClaimLine(
    string ClaimLineId,
    string ValuationClaimId,
    string ValuationLineItemId,
    decimal PercentComplete,    // cumulative % entered this claim
    decimal CumulativeClaimed,  // PercentComplete% x LineAmount
    decimal PeriodIncrement);   // CumulativeClaimed - the line's cumulative on the claim immediately
                                // before (whatever its status); the API's ClaimPeriodBaseline rule

// One line of a valuation's STATEMENT — the client-facing form of the valuation. For a locked
// valuation these are its own frozen rows (the bill line's description, code, quantity, rate and
// amount copied by value at lock, with the % complete and money claimed), so later edits or
// deletions of the live bill never disturb what the client was sent. For a Draft the same shape
// is computed from the live bill on every read (the working copy). ValuationLineItemId is
// provenance only — never a live FK; the line it names may since have been re-priced or removed.
public sealed record ValuationStatementLine(
    string ValuationClaimId,
    string ValuationLineItemId,
    ValuationElementType ElementType,
    string SectionCode,
    string SectionName,
    string VariationRef,
    string VariationTitle,
    ValuationLineType LineType,
    string CostCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal LineAmount,
    decimal PercentComplete,
    decimal CumulativeClaimed,
    decimal PeriodIncrement,
    string Comments,
    int DisplayOrder,
    // The client's schedule-of-works reference frozen at lock: the line's own when it had one,
    // else the project's ClientCostReferences map entry for the cost centre. Empty when neither.
    string ClientReference = "") : IVariationBillLine
{
    public bool CountsTowardTotals => LineType is not (ValuationLineType.Declined or ValuationLineType.Tbc);
}

// A valuation as a statement: the claim (its footer is the statement's summary — frozen when
// locked, computed for the working copy) with its lines in statement order. IsDraft says which:
// a working copy of a Draft valuation prints its "not an issued statement" stamps, a locked one
// is the record the client was sent. AsAt is when the lines were frozen (LockedAt) or, for a
// working copy, when it was computed. One record feeds the on-screen viewer, the PDF, the
// spreadsheet, the emailed attachment and the connector's get_valuation_statement.
public sealed record ValuationStatement(
    ValuationClaim Claim,
    IReadOnlyList<ValuationStatementLine> Lines,
    bool IsDraft,
    DateTimeOffset AsAt)
{
    // "Valuation 05 - September 2026", or "… — working copy" while the valuation is a Draft.
    public string Label => IsDraft ? $"{Claim.DisplayName} — working copy" : Claim.DisplayName;
}

/// <summary>
/// The outcome of emailing a valuation statement from the shared mailbox: which valuation the
/// attached PDF printed, who the draft is addressed to (the project's Client and Architect
/// contacts), and where to open it. <see cref="WebLink"/> opens the draft in Outlook on the web
/// when Graph returns one (it usually does); null otherwise — the draft is still in the mailbox's
/// Drafts folder. Mirrors <see cref="SubcontractorStatementEmailOutcome"/>.
/// </summary>
public sealed record ValuationStatementEmailOutcome(
    string ValuationClaimId,
    string Label,
    string Subject,
    IReadOnlyList<string> RecipientEmails,
    string? WebLink,
    // The staged message's mailbox id — the handle for withdrawing a draft staged in error
    // (DeleteMailboxDraft); null only on legacy payloads.
    string? DraftMessageId = null,
    // Sent=true means the client has it. Sent=false is a draft waiting in the mailbox's Drafts
    // folder — by choice when FailureNote is null, because the send was refused when it is not.
    bool Sent = false,
    string? FailureNote = null);
