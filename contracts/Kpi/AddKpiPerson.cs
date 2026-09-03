using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Kpi;

// Add someone KPIs can be filed under who has no portal login — by name alone ("James Clark").
// Email is optional: give a portal user's sign-in email to link the person to their account
// instead (then their KpiPerson is that link, found or created). Adding a name that already
// exists (case-insensitive) answers with the existing person rather than a twin. Administrators
// only.
public sealed record AddKpiPerson(string Name, string? Email = null) : ICommand<KpiPerson>;
