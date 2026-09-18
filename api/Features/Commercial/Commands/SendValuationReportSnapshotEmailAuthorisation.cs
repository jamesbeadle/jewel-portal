using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class SendValuationReportSnapshotEmailAuthorisation
{
    // Valuation statements are client-facing money correspondence: the circle that runs the
    // valuation report and its claims (mirrors the snapshot take/delete gate), not the wider
    // internal-read set that may merely view snapshots.
    // Borrowed by ValuationSnapshotEmailComposer so the preview reaches exactly who the send does.
    internal static readonly RoleSet RolesThatMayEmailSnapshots = RoleSet.Of(
        JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, SendValuationReportSnapshotEmail command) =>
        RolesThatMayEmailSnapshots.IncludesAny(user.Roles);
}
