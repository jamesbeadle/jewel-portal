using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow;

/// <summary>
/// One gate for the whole feature, reads and writes alike: the Weekly Cashflow is the
/// accountant's working tool (decision 2026-08-27), so Accounts stands beside the directors —
/// the first API surface where it does. The BANK BALANCE line is not widened by this: it comes
/// from GetXeroCashSummary, whose directors-only gate is untouched, and the page only draws the
/// balance for those who can read it.
/// </summary>
internal static class WeeklyCashflowGates
{
    public static readonly RoleSet WeeklyCashflowRoles = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Accounts);
}
