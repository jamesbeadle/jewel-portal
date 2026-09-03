using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// The allocation queue's gate as a class (2026-09-03): the HTTP endpoint checks
/// <see cref="XeroLedgerRoles.AllowedToAllocate"/> inline, but the connector's action gateway
/// composes Authorisation.Allows → Validation.Check → handler by convention, so the same role
/// set is exposed here for the set_xero_allocation action. One source of truth — this class
/// reads the endpoint's own RoleSet rather than copying it.
/// </summary>
public sealed class SetXeroAllocationAuthorisation
{
    public bool Allows(SignedInUser user, SetXeroAllocation command) =>
        XeroLedgerRoles.AllowedToAllocate.IncludesAny(user.Roles);
}
