using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.TenderEnquiries;

/// <summary>
/// Who does what with tender enquiries. The whole internal team reads them; the people who run
/// bids — directors, project managers, the QS, the office — log, edit and answer them; only a
/// director or project manager decides the enquiry's fate (accept, decline, won, lost), the same
/// gate the CRM's bid decision carries.
/// </summary>
internal static class TenderEnquiryRoles
{
    public static readonly RoleSet Readers = JpmsRoleSets.AllInternal;

    public static readonly RoleSet Managers = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,
        JpmsRoles.OfficeComplianceCoordinator,
        JpmsRoles.OfficeAdmin);

    public static readonly RoleSet Deciders = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.ProjectManager);

    /// <summary>The status moves that are a bid decision rather than bookkeeping.</summary>
    public static bool IsDecision(TenderEnquiryStatus status) =>
        status is TenderEnquiryStatus.Accepted
            or TenderEnquiryStatus.Declined
            or TenderEnquiryStatus.Won
            or TenderEnquiryStatus.Lost;
}
