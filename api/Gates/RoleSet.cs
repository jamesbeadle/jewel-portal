using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Gates;

public sealed class RoleSet
{
    private readonly HashSet<Role> allowedRoles;

    public RoleSet(params Role[] allowedRoles)
    {
        this.allowedRoles = new HashSet<Role>(allowedRoles);
    }

    public bool IncludesAny(IEnumerable<Role> roles) => roles.Any(allowedRoles.Contains);

    public static RoleSet Of(params Role[] allowedRoles) => new(allowedRoles);
}

public static class JpmsRoles
{
    public const Role Director = Role.ManagingDirector;
    public const Role FinanceDirector = Role.FinanceDirector;
    public const Role ProjectManager = Role.ProjectManager;
    public const Role Estimator = Role.QuantitySurveyor;
    public const Role SiteManager = Role.SiteManager;
    public const Role HealthAndSafetyLead = Role.HealthSafetyOfficer;
    public const Role OfficeComplianceCoordinator = Role.OfficeComplianceCoordinator;
    public const Role Architect = Role.Architect;
    public const Role Client = Role.Client;
    public const Role Subcontractor = Role.Subcontractor;
    public const Role Foreman = Role.Foreman;
    public const Role SiteOperative = Role.SiteOperative;
}
