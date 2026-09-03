using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Kpi;

// Take the KPI mark off an email — deletes the row. The email itself is untouched (it was never
// tagged); the person stays on the list. Administrators only.
public sealed record RemoveKpiEmail(string KpiEmailId) : ICommand<Acknowledgement>;
