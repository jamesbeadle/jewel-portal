namespace Jewel.JPMS.Api.Gates;

public sealed class RoleSet
{
    private readonly HashSet<string> allowedRoles;

    public RoleSet(params string[] allowedRoles)
    {
        this.allowedRoles = new HashSet<string>(allowedRoles, StringComparer.OrdinalIgnoreCase);
    }

    public bool Includes(string role) => allowedRoles.Contains(role);

    public static RoleSet Of(params string[] allowedRoles) => new(allowedRoles);
}

public static class JpmsRoles
{
    public const string Director = "P01";
    public const string FinanceDirector = "P02";
    public const string ProjectManager = "P03";
    public const string Estimator = "P04";
    public const string SiteManager = "P05";
    public const string HealthAndSafetyLead = "P06";
    public const string OfficeComplianceCoordinator = "P07";
    public const string Architect = "P08";
    public const string Client = "P09";
    public const string Subcontractor = "P10";
    public const string Foreman = "P11";
}
