using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Re-issuing a request document to external parties is the same calibre of action as raising one,
// so it is restricted to the internal staff and architects who own the request lifecycle.
public sealed class ResendRequestDocumentAuthorisation
{
    private static readonly RoleSet RolesThatMayResend =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.SiteManager, JpmsRoles.Architect);

    public bool Allows(SignedInUser user, ResendRequestDocument command) => RolesThatMayResend.IncludesAny(user.Roles);
}
