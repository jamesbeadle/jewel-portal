using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

public sealed class SetTenderEnquiryAnswersAuthorisation
{
    public bool Allows(SignedInUser user, SetTenderEnquiryAnswers command) =>
        TenderEnquiryRoles.Managers.IncludesAny(user.Roles);
}
