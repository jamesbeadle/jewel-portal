using Jewel.JPMS.Api.Gates;

namespace Jewel.JPMS.Api.Features.DocumentControl;

/// <summary>
/// Who may work the Document Control queue — sending attachments in from the Control Centre,
/// filing them out, discarding and restoring. The circle that ran the retired save-to-drawings
/// import (ImportDrawingFromMessageAuthorisation's set) plus the Finance Director, who sits in the
/// triage circle and files payment certificates. Administrators pass via role expansion.
/// </summary>
internal static class DocumentControlRoles
{
    public static readonly RoleSet AllowedToManage = RoleSet.Of(
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin);

    /// <summary>Who may read the payment certificate register — the money-facing circle
    /// (mirrors the finance reads' CommercialTeam gate).</summary>
    public static readonly RoleSet AllowedToReadPaymentCertificates = JpmsRoleSets.CommercialTeam;
}
