using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.CostCenters;

// Global query — the cost-center master is not scoped to a project.
// IncludeInactive is used by the cost-code admin page; everything else
// (Financials, valuation dropdowns) wants active codes only.
public sealed record ListCostCenters(bool IncludeInactive = false) : IQuery<IReadOnlyList<CostCenter>>;
