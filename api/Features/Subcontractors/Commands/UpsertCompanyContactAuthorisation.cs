using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpsertCompanyContactAuthorisation
{
    // Mirrors UpdateSubcontractor's gate — a record's contact list is part of editing the record.
    private static readonly RoleSet RolesThatMayEditContacts =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.ProjectManager);

    public bool Allows(SignedInUser user, UpsertCompanyContact command) => RolesThatMayEditContacts.IncludesAny(user.Roles);

    public bool Allows(SignedInUser user, RemoveCompanyContact command) => RolesThatMayEditContacts.IncludesAny(user.Roles);
}
