using Jewel.JPMS.Contracts.Inventory;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

public sealed class UpdateInventoryItemAuthorisation
{
    private static readonly RoleSet RolesThatMayUpdateInventory =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, UpdateInventoryItem command) => RolesThatMayUpdateInventory.IncludesAny(user.Roles);
}
