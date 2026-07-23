namespace Jewel.JPMS.Models;

// Lifecycle of a subcontractor-raised variation request. The sub proposes and prices a change
// against one of their work orders; JBB reviews it. Acceptance creates a variation order (in
// Quoting, with the sub's price as the estimate and the sub as the selected subcontractor) that
// then runs the normal Issued → Approved lifecycle; a new work order is issued from the approved
// VO (existing WOs are never uplifted).
public enum VariationRequestStatus
{
    Submitted = 0,
    UnderReview = 1,
    Accepted = 2,
    Rejected = 3,
    Withdrawn = 4
}

public sealed record SubcontractorVariationRequest(
    string VariationRequestId,
    string ProjectId,
    string WorkOrderId,
    string SubcontractorId,
    string Title,
    string Description,
    decimal ProposedValue,
    VariationRequestStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt = null,
    string? ReviewedByEmail = null,
    string RejectionReason = "",
    string? VariationOrderId = null,
    // Display helpers resolved server-side so portal sessions never join internal lists.
    string ProjectName = "",
    int WorkOrderNumber = 0,
    string SubcontractorName = "")
{
    public bool IsOpen => Status is VariationRequestStatus.Submitted or VariationRequestStatus.UnderReview;
}
