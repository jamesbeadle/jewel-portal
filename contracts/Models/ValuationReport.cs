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
    int DisplayOrder)
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
    decimal PaymentDueExVat);

// Per claim, per line item: the cumulative % complete entered and the resulting amounts.
public sealed record ClaimLine(
    string ClaimLineId,
    string ValuationClaimId,
    string ValuationLineItemId,
    decimal PercentComplete,    // cumulative % entered this claim
    decimal CumulativeClaimed,  // PercentComplete% x LineAmount
    decimal PeriodIncrement);   // CumulativeClaimed - previous confirmed cumulative for this line
