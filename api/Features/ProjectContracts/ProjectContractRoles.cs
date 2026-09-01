using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.ProjectContracts;

internal static class ProjectContractRoles
{
    /// <summary>
    /// Who may record or amend contract terms. Deliberately narrow — these figures are the basis of
    /// every valuation, notice and variation argument on the project, and a wrong retention percent
    /// or completion date propagates silently into correspondence with the client.
    /// </summary>
    public static readonly RoleSet AllowedToManageContract =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, Role.FinanceDirector, JpmsRoles.Estimator);

    /// <summary>
    /// Who may read them. Wider: a site manager needs the completion date, a PM needs the notice
    /// periods. Externals are excluded — the contract sum is not theirs to read here.
    /// </summary>
    public static readonly RoleSet AllowedToReadContract = JpmsRoleSets.AllInternal;
}
