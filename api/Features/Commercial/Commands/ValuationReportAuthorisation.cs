using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

// Authorisation for the Valuation Report write surface. Two distinct rights live here:
// editing the bill itself (adding/updating/removing valuation line items) and moving a
// claim through its lifecycle. They used to share one role set; they are now split so the
// Finance Director — the documented owner of the financial output side of valuations
// (permissions matrix, workflow 07) — can run the claim lifecycle without also gaining
// the right to rebuild the bill of quantities.
public sealed class ValuationReportAuthorisation
{
    // Editing the bill of quantities that backs a valuation — adding, updating and removing
    // valuation line items. This stays with the estimating/commercial drafters; the Finance
    // Director does not rebuild the bill.
    private static readonly RoleSet RolesThatMayEditValuationBill =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator);

    // Moving a claim through its lifecycle — starting, preapproving, confirming, reopening,
    // renaming and deleting claims. The Finance Director joins the drafters here: they own
    // the financial output side of valuations and drive claims end to end.
    private static readonly RoleSet RolesThatMayManageClaimLifecycle =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.FinanceDirector);

    // Snapshots are a commercial/finance record (they back invoice submissions), so the
    // Finance Director can take and delete them too — matching who may manage the
    // valuation invoices themselves.
    private static readonly RoleSet RolesThatMayManageSnapshots =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.FinanceDirector);

    // Recoding which cost centre a variation's value sits against is a financial correction
    // to records frozen at VO approval: the MD, FD and project manager only (administrators
    // pass every gate — SignedInUserResolver grants them all roles).
    private static readonly RoleSet RolesThatMayRecodeCostCentres =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    // Recording a claim entry sets a line's completion percentage — the commercial input
    // that drives what is claimed this period. The Finance Director joins the valuation
    // maintainers here (they own the financial output side of valuations).
    private static readonly RoleSet RolesThatMayRecordClaimEntries =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.FinanceDirector);

    private bool MayEditBill(SignedInUser user) => RolesThatMayEditValuationBill.IncludesAny(user.Roles);
    private bool MayManageClaimLifecycle(SignedInUser user) => RolesThatMayManageClaimLifecycle.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, AddValuationLineItem command) => MayEditBill(user);
    public bool Allows(SignedInUser user, UpdateValuationLineItem command) => MayEditBill(user);
    public bool Allows(SignedInUser user, RemoveValuationLineItem command) => MayEditBill(user);
    public bool Allows(SignedInUser user, StartValuationClaim command) => MayManageClaimLifecycle(user);
    public bool Allows(SignedInUser user, RecordClaimEntry command) => RolesThatMayRecordClaimEntries.IncludesAny(user.Roles);
    // Bulk entry is the same act as single entry, just batched — identical gate.
    public bool Allows(SignedInUser user, RecordClaimEntries command) => RolesThatMayRecordClaimEntries.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, PreapproveValuationClaim command) => MayManageClaimLifecycle(user);
    public bool Allows(SignedInUser user, ReopenValuationClaim command) => MayManageClaimLifecycle(user);
    public bool Allows(SignedInUser user, ConfirmValuationClaim command) => MayManageClaimLifecycle(user);
    // Naming and deleting claims are claim-lifecycle actions — same gate as starting one.
    public bool Allows(SignedInUser user, RenameValuationClaim command) => MayManageClaimLifecycle(user);
    public bool Allows(SignedInUser user, DeleteValuationClaim command) => MayManageClaimLifecycle(user);
    public bool Allows(SignedInUser user, SetValuationLineCostCentre command) => RolesThatMayRecodeCostCentres.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, TakeValuationReportSnapshot command) => RolesThatMayManageSnapshots.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, DeleteValuationReportSnapshot command) => RolesThatMayManageSnapshots.IncludesAny(user.Roles);
}
