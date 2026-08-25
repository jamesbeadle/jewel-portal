using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class LogTenderEnquiryFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, LogTenderEnquiryFromMessage command) =>
        TenderEnquiryRoles.Managers.IncludesAny(user.Roles);
}
