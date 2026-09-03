namespace Jewel.JPMS.Features.Kpi;

/// <summary>
/// Who a KPI is filed under, as the picker hands it to a command: exactly one of an existing
/// KPI person's id, a portal user's sign-in email, or a bare name for someone without a login.
/// Mirrors the three ways MarkEmailAsKpi / UpdateKpiEmail name a person.
/// </summary>
public sealed record KpiPersonChoice(string? PersonId = null, string? Email = null, string? Name = null)
{
    public bool IsEmpty => PersonId is null && Email is null && Name is null;
}
