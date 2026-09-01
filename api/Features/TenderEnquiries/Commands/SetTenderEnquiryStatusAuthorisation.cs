using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>Bookkeeping moves (PQQ sent, shortlisted, tender sent) belong to whoever runs the bid;
/// the decisions — accept, decline, won, lost — to a director or project manager.</summary>
public sealed class SetTenderEnquiryStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetTenderEnquiryStatus command) =>
        TenderEnquiryRoles.IsDecision(command.Status)
            ? TenderEnquiryRoles.Deciders.IncludesAny(user.Roles)
            : TenderEnquiryRoles.Managers.IncludesAny(user.Roles);
}
