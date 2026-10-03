namespace Jewel.JPMS.Models;

/// <summary>
/// The bank position — every account's balance and statement balance — is the company's most
/// sensitive figure: directors only, deliberately tighter than the ledger and the Weekly
/// Cashflow's audience. Reading it and keying a statement balance share the one gate.
/// </summary>
public static class BankPositionGates
{
    public static readonly RoleSet BankPositionRoles = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector);

    public static bool Admits(IEnumerable<Role> roles) => BankPositionRoles.IncludesAny(roles);
}
