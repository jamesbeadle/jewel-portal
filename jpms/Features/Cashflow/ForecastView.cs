using Jewel.JPMS.Commercial;

namespace Jewel.JPMS.Features.Cashflow;

/// <summary>The forecast as the page renders it — built once per render from the statement
/// figures and threaded through the KPI strip, the table, the chart and the reconciliation
/// check, so all of them describe the same forecast.</summary>
public sealed record ForecastView(
    DateTime[] Axis,
    IReadOnlyDictionary<ForecastCategory, decimal[]> Cells,
    IReadOnlyDictionary<ForecastCategory, decimal> Later,
    IReadOnlyDictionary<ForecastCategory, decimal> Undated,
    IReadOnlyDictionary<ForecastCategory, List<(Project Project, PhasedCategory Phased)>> PerProject,
    decimal[] ProjectNet,
    decimal[] Net,
    decimal LaterNet,
    decimal[] Closing,
    int MinIndex,
    List<(Project Project, decimal Variance)> Variances);
