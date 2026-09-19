using Jewel.JPMS.Api.Features.ClientPortal;

namespace Jewel.JPMS.Api.Features.Variations;

/// <summary>
/// Which variation order a caller may act on. The role gate answers "may this kind of user approve
/// a variation?"; this answers "is this one theirs?".
///
/// Internal roles work every project, so their role is the whole answer. A client is not:
/// AllowedToApproveVariations admits Role.Client for ANY order id, and approval writes the contract
/// figures — so without this a signed-in client could approve or reject another client's variation
/// (found by the permission check, 2026-09-19). The ownership rule is the client portal's own
/// ClientProjects, so the two surfaces answer the same question the same way.
/// </summary>
internal static class VariationOrderScope
{
    public static async Task<bool> IsTheirsToActOnAsync(
        JpmsContext context, SignedInUser user, string variationOrderId,
        CancellationToken cancellationToken)
    {
        if (VariationRoles.AllowedToManageVariations.IncludesAny(user.Roles)) return true;

        var clientId = ClientScope.OwnClientId(user);
        if (clientId is null) return false;

        return await ClientProjects.OwnsVariationOrderAsync(
            context, clientId, variationOrderId, cancellationToken);
    }
}
