using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class DeleteDraftWorkOrderAuthorisation
{
    // The same roles that may approve or reject a draft: deleting one is the same
    // decision taken about a record that should never have existed.
    private static readonly RoleSet RolesThatMayDeleteDrafts =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, DeleteDraftWorkOrder command) =>
        RolesThatMayDeleteDrafts.IncludesAny(user.Roles);
}
