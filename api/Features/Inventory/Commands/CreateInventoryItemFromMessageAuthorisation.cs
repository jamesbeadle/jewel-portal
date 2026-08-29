using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Inventory;

namespace Jewel.JPMS.Api.Features.Inventory.Commands;

// Mirrors AddInventoryItemAuthorisation: the Control Centre is an internal surface and the add
// roles are already internal-only, so the from-mail set is the same.
public sealed class CreateInventoryItemFromMessageAuthorisation
{
    private static readonly RoleSet RolesThatMayAddInventoryFromMail =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager);
    public bool Allows(SignedInUser user, CreateInventoryItemFromMessage command) =>
        RolesThatMayAddInventoryFromMail.IncludesAny(user.Roles);
}
