using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>Setting a password by hand is the administrator's act alone — narrower than the admin
/// gate the directory shares with the finance director, because whoever sets a password knows it.</summary>
public sealed class SetUserPasswordAuthorisation
{
    public bool Allows(SignedInUser user, SetUserPassword command) =>
        JpmsRoleSets.Administrators.IncludesAny(user.Roles);
}
