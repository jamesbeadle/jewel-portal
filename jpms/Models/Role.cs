namespace Jewel.JPMS.Models;

public enum Role
{
    Admin,
    ManagingDirector,
    FinanceDirector,
    ProjectManager,
    QuantitySurveyor,
    SiteManager,
    HealthSafetyOfficer,
    OfficeComplianceCoordinator,
    Architect,
    Client,
    Subcontractor,
    Foreman
}

public static class RoleExtensions
{
    public static string DisplayName(this Role role) => RolePresentations.For(role).DisplayName;

    public static string PersonaCode(this Role role) => RolePresentations.For(role).PersonaCode;

    public static string AccentDotClass(this Role role) => RolePresentations.For(role).AccentDotClass;

    public static bool IsAdministrative(this Role role) => role == Role.Admin;

    public static bool IsExternal(this Role role) =>
        role is Role.Architect or Role.Client or Role.Subcontractor;
}
