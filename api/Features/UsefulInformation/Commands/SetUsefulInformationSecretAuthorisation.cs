using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Api.Features.UsefulInformation.Commands;

// Holding a credential against a note is managing the note — the site manager who set the WiFi
// code writes it down; reading it back is the directors' (RevealUsefulInformationSecret).
public sealed class SetUsefulInformationSecretAuthorisation
{
    public bool Allows(SignedInUser user, SetUsefulInformationSecret command) =>
        UsefulInformationRoles.AllowedToManage.IncludesAny(user.Roles);
}
