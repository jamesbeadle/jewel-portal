
namespace Jewel.JPMS.Api.Features.Requests;

// Mailbox triage is an internal Jewel back-office task: deciding which project request an
// inbound email belongs to (or that it should be discarded). For now it is restricted to
// administrators, the directors, and project managers. The managing director is here because
// the triage backlog is surfaced on his dashboard (RoleHome's "to triage" tile) — a count he
// can see but not open is worse than no count. Administrators are granted every role
// server-side, so they pass this gate via Role.Admin. A dedicated triage-visibility role can
// be added later. Mirrored by DesktopNavigation.TriageRoles — keep the two lists in step.
internal static class TriageRoles
{
    public static readonly RoleSet AllowedToTriage =
        RoleSet.Of(
            Role.Admin,
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.FinanceDirector);
}
