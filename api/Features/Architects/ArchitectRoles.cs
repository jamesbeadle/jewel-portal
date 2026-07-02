using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Architects;

// Managing architect practices (and the contact email that RFIs are issued to when an architect is
// a project's party) is a back-office task, restricted to the same set that manages client
// accounts: Administrator, Managing Director and Project Manager. Administrators pass via
// Role.Admin (granted every role server-side).
internal static class ArchitectRoles
{
    public static readonly RoleSet AllowedToManageArchitects =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);
}
