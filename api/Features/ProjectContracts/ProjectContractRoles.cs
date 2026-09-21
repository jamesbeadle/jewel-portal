
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
    /// Who may read them. The homeowner's name, the contract sum, the LD rates and the executed
    /// PDF are commercial facts, so this is the money-facing set the payment certificates use,
    /// plus the administrator (Nigel, 2026-09-21 — it was every internal role until then, which
    /// put the contract sum in front of the foreman, H&amp;S, sales and accounts). A site manager
    /// who needs the completion date asks the project manager for it.
    /// </summary>
    public static readonly RoleSet AllowedToReadContract =
        RoleSet.Of(Role.Admin, JpmsRoles.Director, Role.FinanceDirector, JpmsRoles.ProjectManager, JpmsRoles.Estimator);
}
