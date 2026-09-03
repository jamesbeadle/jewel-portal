using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Kpi;

// The KPI register, newest-marked first — everyone's, or one person's when PersonId is given.
// Administrators only: the endpoint refuses every other role outright.
public sealed record ListKpiEmails(string? PersonId = null) : IQuery<IReadOnlyList<KpiEmail>>;

// The people KPIs can be filed under — every KpiPerson (portal users who have been filed against
// and people added by name), each with how many KPIs they carry. Administrators only.
public sealed record ListKpiPeople() : IQuery<IReadOnlyList<KpiPerson>>;
