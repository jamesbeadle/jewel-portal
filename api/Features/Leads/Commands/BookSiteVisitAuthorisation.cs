using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class BookSiteVisitAuthorisation
{
    private static readonly RoleSet RolesThatMayBookSiteVisits =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, BookSiteVisit command) =>
        RolesThatMayBookSiteVisits.IncludesAny(user.Roles);
}
