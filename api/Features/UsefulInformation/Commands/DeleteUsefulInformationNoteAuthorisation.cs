using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

public sealed class DeleteUsefulInformationNoteAuthorisation
{
    public bool Allows(SignedInUser user, DeleteUsefulInformationNote command) =>
        UsefulInformationRoles.AllowedToManage.IncludesAny(user.Roles);
}
