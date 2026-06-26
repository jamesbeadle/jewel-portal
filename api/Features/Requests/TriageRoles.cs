using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Requests;

// Mailbox triage is an internal Jewel back-office task: deciding which project request an
// inbound email belongs to (or that it should be discarded). It is deliberately NOT open to
// external participants (architects, subcontractors, clients) who may be raising requests.
internal static class TriageRoles
{
    public static readonly RoleSet AllowedToTriage =
        RoleSet.Of(
            JpmsRoles.Director,
            JpmsRoles.ProjectManager,
            JpmsRoles.SiteManager,
            JpmsRoles.Estimator,
            JpmsRoles.HealthAndSafetyLead,
            JpmsRoles.OfficeComplianceCoordinator,
            JpmsRoles.Foreman);
}
