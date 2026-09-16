using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

/// <summary>Whoever assembles progress reports assembles the Contractor's Report.</summary>
public sealed class CreateContractorsReportAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, CreateContractorsReport command) => Allows(user);
}

public sealed class UpdateContractorsReportAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, UpdateContractorsReport command) => Allows(user);
}

public sealed class DeleteContractorsReportAuthorisation
{
    public bool Allows(SignedInUser user) => ProgressRoles.Contributors.IncludesAny(user.Roles);
    public bool Allows(SignedInUser user, DeleteContractorsReport command) => Allows(user);
}
