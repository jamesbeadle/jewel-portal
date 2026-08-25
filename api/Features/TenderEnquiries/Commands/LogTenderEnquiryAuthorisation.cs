using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryAuthorisation
{
    public bool Allows(SignedInUser user, LogTenderEnquiry command) =>
        TenderEnquiryRoles.Managers.IncludesAny(user.Roles);
}
