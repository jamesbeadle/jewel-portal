using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryCloseoutStore : ICloseoutStore
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    private readonly List<Defect> defects = new()
    {
        new("DF-001", "PRJ-001", "Skirting paint touch-up to master bedroom", "Master bedroom", NigelEmail, DefectStatus.Open,       DateTimeOffset.UtcNow.AddDays(-3),  null),
        new("DF-002", "PRJ-001", "Lock alignment on rear utility door",        "Utility room",   NigelEmail, DefectStatus.InProgress, DateTimeOffset.UtcNow.AddDays(-5),  null),
        new("DF-003", "PRJ-001", "Outlet trim missing in kitchen island",      "Kitchen",        NigelEmail, DefectStatus.Resolved,   DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-7))
    };

    private readonly Dictionary<string, SettlementRecord> settlements = new();
    private readonly Dictionary<string, VatAnalysis> vatAnalyses = new();
    private readonly Dictionary<string, RetentionRelease> retentions = new();

    public event Action? OnChange;

    public IReadOnlyList<Defect> DefectsFor(string projectId) =>
        defects.Where(d => string.Equals(d.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
               .OrderByDescending(d => d.RaisedAt).ToList().AsReadOnly();

    public Defect SaveDefect(Defect defect)
    {
        var existing = defects.FirstOrDefault(d => d.DefectId == defect.DefectId);
        if (existing is not null) defects.Remove(existing);
        defects.Add(defect);
        OnChange?.Invoke();
        return defect;
    }

    public SettlementRecord? SettlementFor(string projectId) =>
        settlements.TryGetValue(projectId, out var settlement) ? settlement : null;

    public SettlementRecord SaveSettlement(SettlementRecord settlement)
    {
        settlements[settlement.ProjectId] = settlement;
        OnChange?.Invoke();
        return settlement;
    }

    public VatAnalysis? VatFor(string projectId) =>
        vatAnalyses.TryGetValue(projectId, out var analysis) ? analysis : null;

    public VatAnalysis SaveVat(VatAnalysis analysis)
    {
        vatAnalyses[analysis.ProjectId] = analysis;
        OnChange?.Invoke();
        return analysis;
    }

    public RetentionRelease? RetentionFor(string projectId) =>
        retentions.TryGetValue(projectId, out var release) ? release : null;

    public RetentionRelease SaveRetention(RetentionRelease release)
    {
        retentions[release.ProjectId] = release;
        OnChange?.Invoke();
        return release;
    }
}
