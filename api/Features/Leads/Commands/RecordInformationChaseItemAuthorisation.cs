using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordInformationChaseItemAuthorisation
{
    private static readonly RoleSet RolesThatMayChaseInformation =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    public bool Allows(SignedInUser user, RecordInformationChaseItem command) =>
        RolesThatMayChaseInformation.IncludesAny(user.Roles);
}
