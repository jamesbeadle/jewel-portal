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
    Foreman,

    // Day-rate site operatives logging their own time on the My Day page. Same account /
    // password / session model as every other user (docs/Labour-Time-Tracking-Scope.md).
    SiteOperative
}
