using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

// ============================================================================
// Server-side shapes for the Xero write-back: confirming an allocated draft
// bill's Sites / Cost Code tracking back onto the Xero invoice and approving
// it (DRAFT → AUTHORISED). Kept out of the shared contracts project — the
// front end only ever sees the outcome stamped on the ledger lines.
// ============================================================================

/// <summary>
/// Everything the Xero client needs to confirm and approve one invoice.
/// One instruction per stored ledger line; lines of the invoice that were
/// never queued (non-cost-of-sales) pass through untouched.
/// </summary>
public sealed record XeroApprovalRequest(
    string InvoiceId,
    bool IsCreditNote,
    IReadOnlyList<XeroApprovalLineInstruction> Lines);

/// <summary>
/// The allocation to stamp on one Xero line item. A single share covers the
/// whole line (tracking set in place); multiple shares replace the Xero line
/// with one line per share — a share carries its own site (project) as well as
/// its cost code, so a split can span projects — amounts pro-rated so the
/// invoice total is unchanged to the penny.
/// </summary>
public sealed record XeroApprovalLineInstruction(
    string LineItemId,
    IReadOnlyList<XeroApprovalShare> Shares);

/// <summary>One share of a line: which Sites option and Cost Code option to stamp, and its net weight.</summary>
public sealed record XeroApprovalShare(string SiteOption, string CostCenterCode, decimal Net);

/// <summary>
/// Everything the Xero client needs to stamp Sites tracking onto specific line
/// items of one invoice WITHOUT approving it — the SetProject half-step, taken
/// when a queued line's project is known before its cost centre. Only the named
/// line items are touched; each keeps whatever other tracking it already has.
/// </summary>
public sealed record XeroSiteTrackingRequest(
    string InvoiceId,
    bool IsCreditNote,
    IReadOnlyList<XeroSiteTrackingLine> Lines);

/// <summary>Which Sites option to stamp on one Xero line item.</summary>
public sealed record XeroSiteTrackingLine(string LineItemId, string SiteOption);

/// <summary>
/// What happened. AlreadyApproved is a success without any write — the
/// invoice was approved in Xero outside JPMS between allocation and now.
/// FreshStatus is the invoice status as Xero reported it during the attempt.
/// </summary>
public sealed record XeroApprovalResult(
    bool Succeeded,
    bool AlreadyApproved,
    string? FreshStatus,
    string? Error)
{
    public static XeroApprovalResult Ok(string status) => new(true, false, status, null);
    public static XeroApprovalResult SkippedAlreadyApproved(string status) => new(true, true, status, null);
    public static XeroApprovalResult Failed(string error) => new(false, false, null, error);
}

// Penny-safe pro-rating of a Xero line amount across cost-centre splits lives in
// XeroSplitMaths (contracts project, next to the other tested calculation helpers).
