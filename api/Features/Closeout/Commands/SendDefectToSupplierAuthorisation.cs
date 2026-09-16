using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

// The defect page offers Send to supplier to every internal role (ProjectDefectDetail.CanSend),
// and the send itself is SendMailboxEmailAuthorisation's — every internal role. Same gate here.
public sealed class SendDefectToSupplierAuthorisation
{
    public bool Allows(SignedInUser user, SendDefectToSupplier command) =>
        JpmsRoleSets.AllInternal.IncludesAny(user.Roles);
}
