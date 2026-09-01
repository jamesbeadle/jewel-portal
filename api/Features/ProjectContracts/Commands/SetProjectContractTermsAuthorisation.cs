using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

public sealed class SetProjectContractTermsAuthorisation
{
    public bool Allows(SignedInUser user) =>
        ProjectContractRoles.AllowedToManageContract.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, SetProjectContractTerms command) => Allows(user);
}
