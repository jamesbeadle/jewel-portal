using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordLeadQualificationScoreAuthorisation
{
    private static readonly RoleSet RolesThatMayQualifyLeads =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecordLeadQualificationScore command) =>
        RolesThatMayQualifyLeads.Includes(user.Role);
}
