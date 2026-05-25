using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemorySiteStore : ISiteStore
{
    private readonly List<SiteReport> reports = new()
    {
        new("SR-001", "PRJ-001", DateTimeOffset.UtcNow.AddDays(-7),  "Brickwork to south elevation completed. Roof slating ongoing.", 5, 3, 62m, true),
        new("SR-002", "PRJ-001", DateTimeOffset.UtcNow.AddDays(-14), "External envelope progressing per programme. No major issues.",  5, 4, 55m, true)
    };

    private readonly List<ProgrammeTask> tasks = new()
    {
        new("PT-001", "PRJ-001", "Foundations",      DateTimeOffset.UtcNow.AddDays(-60), DateTimeOffset.UtcNow.AddDays(-40),  100m, "BL-001"),
        new("PT-002", "PRJ-001", "Brickwork",        DateTimeOffset.UtcNow.AddDays(-40), DateTimeOffset.UtcNow.AddDays(-10),   95m, "BL-003"),
        new("PT-003", "PRJ-001", "Roof slating",     DateTimeOffset.UtcNow.AddDays(-15), DateTimeOffset.UtcNow.AddDays(15),    40m, "BL-004"),
        new("PT-004", "PRJ-001", "1st fix electric", DateTimeOffset.UtcNow.AddDays(0),   DateTimeOffset.UtcNow.AddDays(30),    10m, "BL-005")
    };

    public event Action? OnChange;

    public IReadOnlyList<SiteReport> ReportsFor(string projectId) =>
        reports.Where(r => string.Equals(r.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
               .OrderByDescending(r => r.PeriodEnd).ToList().AsReadOnly();

    public IReadOnlyList<ProgrammeTask> ProgrammeFor(string projectId) =>
        tasks.Where(t => string.Equals(t.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
             .OrderBy(t => t.PlannedStart).ToList().AsReadOnly();

    public SiteReport SaveReport(SiteReport report)
    {
        var existing = reports.FirstOrDefault(r => r.SiteReportId == report.SiteReportId);
        if (existing is not null) reports.Remove(existing);
        reports.Add(report);
        OnChange?.Invoke();
        return report;
    }
}
