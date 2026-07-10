using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Authorisation for the Valuation Report write surface. Building the bill, recording
// claims, and moving a claim through its lifecycle are all commercial actions, so the
// same roles that may draft valuations may also maintain the valuation report.
public sealed class ValuationReportAuthorisation
{
    private static readonly RoleSet RolesThatMayMaintainValuations =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Snapshots are a commercial/finance record (they back invoice submissions), so the
    // Finance Director can take and delete them too — matching who may manage the
    // valuation invoices themselves.
    private static readonly RoleSet RolesThatMayManageSnapshots =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.FinanceDirector);

    private bool Allowed(SignedInUser user) => RolesThatMayMaintainValuations.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, AddValuationLineItem command) => Allowed(user);
    public bool Allows(SignedInUser user, UpdateValuationLineItem command) => Allowed(user);
    public bool Allows(SignedInUser user, RemoveValuationLineItem command) => Allowed(user);
    public bool Allows(SignedInUser user, StartValuationClaim command) => Allowed(user);
    public bool Allows(SignedInUser user, RecordClaimEntry command) => Allowed(user);
    public bool Allows(SignedInUser user, PreapproveValuationClaim command) => Allowed(user);
    public bool Allows(SignedInUser user, ConfirmValuationClaim command) => Allowed(user);
    public bool Allows(SignedInUser user, TakeValuationReportSnapshot command) => RolesThatMayManageSnapshots.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, DeleteValuationReportSnapshot command) => RolesThatMayManageSnapshots.IncludesAny(user.Roles);
}
