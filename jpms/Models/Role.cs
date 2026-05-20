namespace Jewel.JPMS.Models;

/// <summary>
/// The user's role on the JPMS platform. Drives what they can see and do (RBAC).
/// One role per user for now — when SQL lands this can become a many-to-many
/// relationship and the consuming code only needs the access checks updated.
/// </summary>
public enum Role
{
    /// <summary>Full administrative access. Manages users, roles, and platform configuration.</summary>
    Admin,

    /// <summary>Executive — Managing Director.</summary>
    ManagingDirector,

    /// <summary>Finance — produces cashflow forecasts and issues cash calls.</summary>
    Accountant,

    /// <summary>Pricing and measurement specialist.</summary>
    QuantitySurveyor,

    /// <summary>External client — issues tenders and approves VOs.</summary>
    Architect,

    /// <summary>Field worker — updates line-item completion, timesheets, RFIs.</summary>
    Subcontractor
}

/// <summary>
/// Display helpers and RBAC checks for <see cref="Role"/>.
/// Centralising this means UI components don't switch on the enum directly.
/// </summary>
public static class RoleExtensions
{
    /// <summary>Friendly display name for the role, e.g. "Managing Director".</summary>
    public static string DisplayName(this Role role) => role switch
    {
        Role.Admin            => "Administrator",
        Role.ManagingDirector => "Managing Director",
        Role.Accountant       => "Accountant",
        Role.QuantitySurveyor => "Quantity Surveyor",
        Role.Architect        => "Architect",
        Role.Subcontractor    => "Subcontractor",
        _ => role.ToString()
    };

    /// <summary>Short uppercase code matching the persona codes in /docs/requirements/personas.md.</summary>
    public static string Code(this Role role) => role switch
    {
        Role.Admin            => "ADM",
        Role.ManagingDirector => "P05",
        Role.Accountant       => "P04",
        Role.QuantitySurveyor => "P02",
        Role.Architect        => "P01",
        Role.Subcontractor    => "P03",
        _ => role.ToString().ToUpperInvariant()
    };

    /// <summary>
    /// Single source of truth for "can this role administer the platform?".
    /// Today only <see cref="Role.Admin"/>; later we may grant MD a subset.
    /// </summary>
    public static bool IsAdministrative(this Role role) => role == Role.Admin;

    /// <summary>Tailwind accent dot class for visual role tagging in the UI.</summary>
    public static string AccentDotClass(this Role role) => role switch
    {
        Role.Admin            => "bg-slate-900",
        Role.ManagingDirector => "bg-rose-500",
        Role.Accountant       => "bg-violet-500",
        Role.QuantitySurveyor => "bg-emerald-500",
        Role.Architect        => "bg-sky-500",
        Role.Subcontractor    => "bg-amber-500",
        _ => "bg-slate-400"
    };
}
