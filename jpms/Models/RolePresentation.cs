namespace Jewel.JPMS.Models;

// BadgeClass is the same accent worn as a readable chip — a tinted fill with the role's colour at
// text weight — so a surface can put the role's NAME in the role's COLOUR (the to-do boards' and
// lists' assignee badges). Kept beside AccentDotClass so the dot, the stripe and the badge can
// never drift onto different colours for one role. Tailwind's scanner needs every class literal,
// hence the strings are written out per role rather than composed.
public sealed record RolePresentation(string DisplayName, string PersonaCode, string AccentDotClass,
    string BadgeClass = "border-line bg-surface-raised text-content-muted");

public static class RolePresentations
{
    private static readonly IReadOnlyDictionary<Role, RolePresentation> Map =
        new Dictionary<Role, RolePresentation>
        {
            [Role.Admin]                       = new("Administrator",              "ADM", "bg-slate-900",   "border-slate-500/40 bg-slate-500/15 text-slate-300"),
            [Role.ManagingDirector]            = new("Director / MD",              "P01", "bg-rose-500",    "border-rose-500/40 bg-rose-500/15 text-rose-300"),
            [Role.FinanceDirector]             = new("Finance Director",           "P02", "bg-violet-500",  "border-violet-500/40 bg-violet-500/15 text-violet-300"),
            [Role.ProjectManager]              = new("Project Manager",            "P03", "bg-indigo-500",  "border-indigo-500/40 bg-indigo-500/15 text-indigo-300"),
            [Role.QuantitySurveyor]            = new("QS / Estimator",             "P04", "bg-emerald-500", "border-emerald-500/40 bg-emerald-500/15 text-emerald-300"),
            [Role.SiteManager]                 = new("Site Manager",               "P05", "bg-orange-500",  "border-orange-500/40 bg-orange-500/15 text-orange-300"),
            [Role.HealthSafetyOfficer]         = new("Health & Safety Officer",    "P06", "bg-red-500",     "border-red-500/40 bg-red-500/15 text-red-300"),
            [Role.OfficeComplianceCoordinator] = new("Compliance",                 "P07", "bg-teal-500",    "border-teal-500/40 bg-teal-500/15 text-teal-300"),
            [Role.Architect]                   = new("Architect / Designer",       "P08", "bg-sky-500",     "border-sky-500/40 bg-sky-500/15 text-sky-300"),
            [Role.Client]                      = new("Client / Homeowner",         "P09", "bg-pink-500",    "border-pink-500/40 bg-pink-500/15 text-pink-300"),
            [Role.Subcontractor]               = new("Subcontractor",              "P10", "bg-amber-500",   "border-amber-500/40 bg-amber-500/15 text-amber-300"),
            [Role.Foreman]                     = new("Foreman / Site Team",        "P11", "bg-lime-500",    "border-lime-500/40 bg-lime-500/15 text-lime-300"),
            [Role.SiteOperative]               = new("Site Operative",             "P12", "bg-amber-500",   "border-amber-500/40 bg-amber-500/15 text-amber-300"),
            [Role.Accounts]                    = new("Accounts",                   "P13", "bg-fuchsia-500", "border-fuchsia-500/40 bg-fuchsia-500/15 text-fuchsia-300"),
            [Role.OfficeAdmin]                 = new("Office Admin",               "P14", "bg-cyan-500",    "border-cyan-500/40 bg-cyan-500/15 text-cyan-300"),
            [Role.SalesMarketing]              = new("Sales & Marketing",          "P15", "bg-rose-500",    "border-rose-500/40 bg-rose-500/15 text-rose-300")
        };

    public static RolePresentation For(Role role) => Map[role];
}
