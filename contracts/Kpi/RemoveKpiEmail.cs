using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Kpi;

// Take the KPI mark off an email — deletes the row. The email keeps its JPMS/Admin tag (an
// administrator did deal with it; the Tagged tab can return it to the queue); the person stays
// on the list. Administrators only.
public sealed record RemoveKpiEmail(string KpiEmailId) : ICommand<Acknowledgement>;
