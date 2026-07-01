using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Variations;

// Raising and managing variation order quotes is a commercial/PM task: Administrator, Managing
// Director, Project Manager and QS (Estimator). Administrators carry every role server-side.
internal static class VariationRoles
{
    public static readonly RoleSet AllowedToManageVariations =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
}
