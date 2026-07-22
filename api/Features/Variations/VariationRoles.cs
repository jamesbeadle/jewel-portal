using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations;

// Raising and managing variation order quotes is a commercial/PM task: Administrator, Managing
// Director, Project Manager and QS (Estimator). Administrators carry every role server-side.
internal static class VariationRoles
{
    public static readonly RoleSet AllowedToManageVariations =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Approving a VOQ (raising the VO onto the contract figures) additionally belongs to the
    // client — variations are ultimately the client's instruction to spend. Internal roles stay
    // PM-and-above (plus QS, who prepares the commercial records the approval writes to).
    public static readonly RoleSet AllowedToApproveVariations =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.Client);
}
