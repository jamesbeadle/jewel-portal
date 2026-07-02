using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Todos;

// Project to-dos are a back-office project-management surface. Directors, project managers and site
// managers may manage them; administrators pass via Role.Admin (they are granted every role
// server-side anyway, mirroring TriageRoles' belt-and-braces inclusion).
internal static class TodoRoles
{
    public static readonly RoleSet AllowedToManageTodos =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.SiteManager);
}
