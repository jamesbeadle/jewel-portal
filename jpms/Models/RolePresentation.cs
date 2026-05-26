namespace Jewel.JPMS.Models;

public sealed record RolePresentation(string DisplayName, string PersonaCode, string AccentDotClass);

public static class RolePresentations
{
    private static readonly IReadOnlyDictionary<Role, RolePresentation> Map =
        new Dictionary<Role, RolePresentation>
        {
            [Role.Admin]                       = new("Administrator",              "ADM", "bg-slate-900"),
            [Role.ManagingDirector]            = new("Director / MD",              "P01", "bg-rose-500"),
            [Role.FinanceDirector]             = new("Finance Director",           "P02", "bg-violet-500"),
            [Role.ProjectManager]              = new("Project Manager",            "P03", "bg-indigo-500"),
            [Role.QuantitySurveyor]            = new("QS / Estimator",             "P04", "bg-emerald-500"),
            [Role.SiteManager]                 = new("Site Manager",               "P05", "bg-orange-500"),
            [Role.HealthSafetyOfficer]         = new("Health & Safety Officer",    "P06", "bg-red-500"),
            [Role.OfficeComplianceCoordinator] = new("Office & Compliance",        "P07", "bg-teal-500"),
            [Role.Architect]                   = new("Architect / Designer",       "P08", "bg-sky-500"),
            [Role.Client]                      = new("Client / Homeowner",         "P09", "bg-pink-500"),
            [Role.Subcontractor]               = new("Subcontractor",              "P10", "bg-amber-500"),
            [Role.Foreman]                     = new("Foreman / Site Team",        "P11", "bg-lime-500")
        };

    public static RolePresentation For(Role role) => Map[role];
}
