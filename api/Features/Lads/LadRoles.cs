using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Lads;

// LADs claims are a commercial/programme surface: directors, project managers and the QS record and
// manage the client's claims. Administrators pass via Role.Admin (they are granted every role
// server-side anyway, mirroring TriageRoles' belt-and-braces inclusion).
internal static class LadRoles
{
    public static readonly RoleSet AllowedToManageLads =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.Estimator);
}
