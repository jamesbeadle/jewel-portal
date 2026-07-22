using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class DeclineBidPackageRecipientAuthorisation
{
    private static readonly RoleSet RolesThatMayManageRecipients =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator);

    public bool Allows(SignedInUser user, DeclineBidPackageRecipient command) => RolesThatMayManageRecipients.IncludesAny(user.Roles);
}
