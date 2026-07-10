using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Merging closes one request and rewrites another's history, so it carries the same weight as
// promotion: Administrator, Managing Director and Project Manager only.
public sealed class MergeRequestsAuthorisation
{
    private static readonly RoleSet RolesThatMayMerge =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, MergeRequests command) =>
        RolesThatMayMerge.IncludesAny(user.Roles);
}
