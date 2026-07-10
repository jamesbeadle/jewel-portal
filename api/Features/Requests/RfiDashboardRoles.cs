using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests;

// The cross-project RFI dashboard spans the whole portfolio, so it is an internal back-office
// view. The gate mirrors the desktop navigation's Projects entry: every internal delivery role
// except Foreman — and never external parties (clients, architects and subcontractors only ever
// see their own project's register). Administrators hold every role server-side and pass via
// Role.Admin.
internal static class RfiDashboardRoles
{
    public static readonly RoleSet AllowedToViewDashboard =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.FinanceDirector,
            JpmsRoles.ProjectManager,
            JpmsRoles.Estimator,
            JpmsRoles.SiteManager,
            JpmsRoles.HealthAndSafetyLead,
            JpmsRoles.OfficeComplianceCoordinator);
}
