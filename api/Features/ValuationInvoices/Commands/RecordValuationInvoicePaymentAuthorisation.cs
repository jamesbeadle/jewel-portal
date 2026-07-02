using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class RecordValuationInvoicePaymentAuthorisation
{
    public bool Allows(SignedInUser user, RecordValuationInvoicePayment command) =>
        ValuationInvoiceRoles.AllowedToManageValuationInvoices.IncludesAny(user.Roles);
}
