using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// Fired by the Control Centre's Apply, so it mirrors the triage roles that reach that page —
// the same people SaveExtractedQuote trusts to record the tender itself.
public sealed class RecordTenderResponseAuthorisation
{
    private static readonly RoleSet RolesThatMayFile =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin);

    public bool Allows(SignedInUser user, RecordTenderResponse command) => RolesThatMayFile.IncludesAny(user.Roles);
}
