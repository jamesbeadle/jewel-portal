using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class UpdateUsefulInformationNoteAuthorisation
{
    public bool Allows(SignedInUser user, UpdateUsefulInformationNote command) =>
        UsefulInformationRoles.AllowedToManage.IncludesAny(user.Roles);
}
