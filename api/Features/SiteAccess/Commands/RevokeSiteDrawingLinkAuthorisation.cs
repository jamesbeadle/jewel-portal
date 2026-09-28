using Jewel.JPMS.Contracts.SiteAccess;

namespace Jewel.JPMS.Api.Features.SiteAccess.Commands;

public sealed class RevokeSiteDrawingLinkAuthorisation
{
    public bool Allows(SignedInUser user, RevokeSiteDrawingLink command) =>
        SiteDrawingLinkRoles.Curators.IncludesAny(user.Roles);
}
