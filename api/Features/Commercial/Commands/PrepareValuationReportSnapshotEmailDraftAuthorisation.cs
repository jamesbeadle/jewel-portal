using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class PrepareValuationReportSnapshotEmailDraftAuthorisation
{
    // Valuation statements are client-facing money correspondence: the circle that runs the
    // valuation report and its claims (mirrors the snapshot take/delete gate), not the wider
    // internal-read set that may merely view snapshots.
    private static readonly RoleSet RolesThatMayEmailSnapshots = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, PrepareValuationReportSnapshotEmailDraft command) =>
        RolesThatMayEmailSnapshots.IncludesAny(user.Roles);
}
