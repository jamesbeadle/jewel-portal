using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests;

// Mailbox triage is an internal Jewel back-office task: deciding which project request an
// inbound email belongs to (or that it should be discarded). For now it is restricted to
// administrators and project managers only. Administrators are granted every role server-side,
// so they pass this gate via Role.Admin. A dedicated triage-visibility role can be added later.
internal static class TriageRoles
{
    public static readonly RoleSet AllowedToTriage =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.ProjectManager);
}
