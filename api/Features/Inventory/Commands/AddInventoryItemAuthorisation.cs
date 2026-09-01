using Jewel.JPMS.Contracts.Inventory;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

// Inventory is an internal supplier-side register: the people who order and handle materials.
// Same internal set as defect maintenance (Director/PM/SiteManager) — no external roles.
public sealed class AddInventoryItemAuthorisation
{
    private static readonly RoleSet RolesThatMayAddInventory =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, AddInventoryItem command) => RolesThatMayAddInventory.IncludesAny(user.Roles);
}
