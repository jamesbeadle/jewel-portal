using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

public sealed class CreateSiteDrawingLinkAuthorisation
{
    public bool Allows(SignedInUser user, CreateSiteDrawingLink command) =>
        SiteDrawingLinkRoles.Curators.IncludesAny(user.Roles);
}
