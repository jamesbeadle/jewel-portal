using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Progress;

/// <summary>
/// Role sets for the progress feature. Site Managers, Project Managers and the Managing Director
/// collate progress updates and assemble reports (the MD may help out); administrators pass every
/// gate. Reads are open to the whole internal team — progress photos are useful well beyond the
/// authors — but never to external roles, because reports are assembled for clients deliberately,
/// not exposed raw.
/// </summary>
internal static class ProgressRoles
{
    public static readonly RoleSet Contributors = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.SiteManager);

    public static readonly RoleSet Readers = JpmsRoleSets.AllInternal;
}
