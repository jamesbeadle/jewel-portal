using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Sales;

/// <summary>Client-side mirrors of SalesRoles.SalesTeam / Deciders — visibility only, the API is the gate.</summary>
public static class SalesAccess
{
    public static bool CanWork(Role? role) => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector
        or Role.ProjectManager or Role.QuantitySurveyor or Role.SalesMarketing;

    public static bool CanDecide(Role? role) => role is Role.Admin or Role.ManagingDirector or Role.FinanceDirector;
}
