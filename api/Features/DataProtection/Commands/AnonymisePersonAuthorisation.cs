using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection.Commands;

/// <summary>Erasure is the administrator's act alone — narrower than the admin gate the
/// directory shares with the finance director.</summary>
public sealed class AnonymisePersonAuthorisation
{
    public bool Allows(SignedInUser user, AnonymisePerson command) =>
        JpmsRoleSets.Administrators.IncludesAny(user.Roles);
}
