using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class UpdateTenderEnquiryDetailsAuthorisation
{
    public bool Allows(SignedInUser user, UpdateTenderEnquiryDetails command) =>
        TenderEnquiryRoles.Managers.IncludesAny(user.Roles);
}
