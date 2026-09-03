using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Kpi;

// Re-file a KPI under a different person and/or rewrite its note. The person is named the same
// three ways as MarkEmailAsKpi (PersonId, else PersonEmail, else PersonName). The email snapshot
// and the reference never change. Administrators only.
public sealed record UpdateKpiEmail(
    string KpiEmailId,
    string? PersonId = null,
    string? PersonEmail = null,
    string? PersonName = null,
    string Note = "") : ICommand<KpiEmail>;
