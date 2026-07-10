using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

// The approval-workflow and amendment commands share the valuation-invoice management roles —
// submitting, recording the client's decision, amending, and cancelling are the same
// commercial/finance activity as raising the invoice.
public sealed class ValuationInvoiceWorkflowAuthorisation
{
    private bool Allowed(SignedInUser user) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, UpdateValuationInvoice command) => Allowed(user);
    public bool Allows(SignedInUser user, SubmitValuationInvoice command) => Allowed(user);
    public bool Allows(SignedInUser user, ApproveValuationInvoice command) => Allowed(user);
    public bool Allows(SignedInUser user, RejectValuationInvoice command) => Allowed(user);
    public bool Allows(SignedInUser user, CancelValuationInvoice command) => Allowed(user);
}
