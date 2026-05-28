using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Leads;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordSiteVisitNotesAuthorisation
{
    private static readonly RoleSet RolesThatMayRecordVisitNotes =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.ProjectManager, JpmsRoles.Estimator, JpmsRoles.SiteManager);

    public bool Allows(SignedInUser user, RecordSiteVisitNotes command) =>
        RolesThatMayRecordVisitNotes.Includes(user.Role);
}
