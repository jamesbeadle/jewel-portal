using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ValuationInvoices;

// Valuation invoices are a commercial/finance activity: Administrator, Managing Director, Finance Director
// and Project Manager. Administrators carry every role server-side.
internal static class ValuationInvoiceRoles
{
    public static readonly RoleSet AllowedToManageValuationInvoices =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);
}
