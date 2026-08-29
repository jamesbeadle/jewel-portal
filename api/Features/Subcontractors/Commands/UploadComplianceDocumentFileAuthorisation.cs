using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

/// <summary>
/// Who may attach a compliance FILE to any subcontractor's record from the office side — the same
/// circle that files Document Triage items to a subcontractor (DocumentControlRoles.AllowedToManage),
/// declared as its own set so the Subcontractors feature doesn't reach into Document Control
/// internals. Administrators pass via role expansion. Portal-scoped subcontractor logins have their
/// own endpoint (UploadMyComplianceDocument, session-scoped to their record) — never this one.
/// </summary>
public sealed class UploadComplianceDocumentFileAuthorisation
{
    private static readonly RoleSet AllowedToUpload = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user) => AllowedToUpload.IncludesAny(user.Roles);
}
