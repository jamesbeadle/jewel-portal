using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class CreateValuationInvoiceAuthorisation
{
    public bool Allows(SignedInUser user, CreateValuationInvoice command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}
