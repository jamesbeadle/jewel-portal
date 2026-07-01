using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Clients;

// Managing client accounts (and the architect email that RFIs are issued to) is a back-office task,
// restricted to Administrator, Managing Director and Project Manager — the same set that manages
// drawings and raises requests. Administrators pass via Role.Admin (granted every role server-side).
internal static class ClientRoles
{
    public static readonly RoleSet AllowedToManageClients =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);
}
