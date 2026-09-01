using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

public sealed class AttachProjectContractAmendmentAuthorisation
{
    public bool Allows(SignedInUser user) =>
        ProjectContractRoles.AllowedToManageContract.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, AttachProjectContractAmendment command) => Allows(user);
}
