using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class DeleteValuationInvoiceAuthorisation
{
    public bool Allows(SignedInUser user, DeleteValuationInvoice command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}
