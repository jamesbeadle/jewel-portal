using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class AddUsefulInformationNoteAuthorisation
{
    public bool Allows(SignedInUser user, AddUsefulInformationNote command) =>
        UsefulInformationRoles.AllowedToManage.IncludesAny(user.Roles);
}
