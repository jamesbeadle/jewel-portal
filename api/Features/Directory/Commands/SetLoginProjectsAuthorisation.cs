using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class SetLoginProjectsAuthorisation
{
    public bool Allows(SignedInUser user, SetLoginProjects command) =>
        AdminGate.Allows(user);
}
