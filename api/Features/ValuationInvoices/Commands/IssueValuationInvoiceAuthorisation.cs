using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class IssueValuationInvoiceAuthorisation
{
    public bool Allows(SignedInUser user, IssueValuationInvoice command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}
