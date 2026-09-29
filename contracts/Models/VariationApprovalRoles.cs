namespace Jewel.JPMS.Models;

/// <summary>
/// Who decides a variation — approves it onto the contract figures, or rejects it. A variation
/// is the client's instruction to spend, given by the client or the architect acting for them
/// (matrix workflow 05: the architect signs off the VO), and taken by the PM and above internally,
/// with the QS who prepares the records the approval writes. A party decides on their own
/// projects alone (VariationOrderScope) and approves as Jewel priced it: the staged build-up,
/// never a cost centre of their choosing. The page and the endpoint both read this.
/// </summary>
public static class VariationApprovalRoles
{
    public static readonly RoleSet Deciders = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.Client,
        JpmsRoles.Architect);
}
