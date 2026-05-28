using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Mobilisation;

namespace Jewel.JPMS.Api.Features.Mobilisation.Commands;

public sealed class UpdateMobilisationChecklistItemAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateMobilisation =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.HealthAndSafetyLead);

    public bool Allows(SignedInUser user, UpdateMobilisationChecklistItem command) => RolesThatMayUpdateMobilisation.Includes(user.Role);
}
