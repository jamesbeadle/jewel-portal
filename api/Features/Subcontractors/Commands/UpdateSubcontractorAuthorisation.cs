using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpdateSubcontractorAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateSubcontractors =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProcurementLead);

    public bool Allows(SignedInUser user, UpdateSubcontractor command) => RolesThatMayUpdateSubcontractors.Includes(user.Role);
}
