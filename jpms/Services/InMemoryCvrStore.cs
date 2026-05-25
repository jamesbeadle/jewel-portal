using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryCvrStore : ICvrStore
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";
    private readonly DateTimeOffset baseDate = DateTimeOffset.UtcNow.AddMonths(-3);

    private readonly List<CvrSnapshot> snapshots;
    private readonly List<CvrPackageRow> packageRows;
    private readonly List<ForecastComponent> forecastComponents;
    private readonly List<QsAccrual> accruals;
    private readonly List<PrelimItem> prelimItems;
    private readonly List<PrelimForecastEntry> prelimEntries;
    private readonly List<Eot> eots;

    public InMemoryCvrStore()
    {
        snapshots = CvrSeed.Snapshots(baseDate);
        packageRows = CvrSeed.PackageRows();
        forecastComponents = CvrSeed.ForecastComponents();
        accruals = CvrSeed.Accruals(NigelEmail);
        prelimItems = CvrSeed.PrelimItems();
        prelimEntries = CvrSeed.PrelimEntries(prelimItems);
        eots = CvrSeed.Eots();
    }

    public event Action? OnChange;

    public IReadOnlyList<CvrSnapshot> SnapshotsFor(string projectId) =>
        snapshots.Where(s => Match(s.ProjectId, projectId))
                 .OrderByDescending(s => s.SnapshotAt).ToList().AsReadOnly();

    public CvrSnapshot? LatestSnapshot(string projectId) =>
        SnapshotsFor(projectId).FirstOrDefault();

    public IReadOnlyList<CvrPackageRow> PackagesFor(string projectId) =>
        packageRows.Where(p => Match(p.ProjectId, projectId)).ToList().AsReadOnly();

    public IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId) =>
        forecastComponents.Where(f => Match(f.ProjectId, projectId)).ToList().AsReadOnly();

    public IReadOnlyList<QsAccrual> AccrualsFor(string projectId) =>
        accruals.Where(a => Match(a.ProjectId, projectId))
                .OrderByDescending(a => a.SignedOffAt).ToList().AsReadOnly();

    public QsAccrual SaveAccrual(QsAccrual accrual)
    {
        var existing = accruals.FirstOrDefault(a => a.QsAccrualId == accrual.QsAccrualId);
        if (existing is not null) accruals.Remove(existing);
        accruals.Add(accrual);
        OnChange?.Invoke();
        return accrual;
    }

    public IReadOnlyList<PrelimItem> PrelimsFor(string projectId) =>
        prelimItems.Where(p => Match(p.ProjectId, projectId)).ToList().AsReadOnly();

    public IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId) =>
        prelimEntries.Where(p => p.PrelimItemId == prelimItemId)
                     .OrderBy(p => p.WeekNumber).ToList().AsReadOnly();

    public IReadOnlyList<Eot> EotsFor(string projectId) =>
        eots.Where(e => Match(e.ProjectId, projectId))
            .OrderByDescending(e => e.GrantedAt).ToList().AsReadOnly();

    public Eot SaveEot(Eot eot)
    {
        var existing = eots.FirstOrDefault(e => e.EotId == eot.EotId);
        if (existing is not null) eots.Remove(existing);
        eots.Add(eot);
        OnChange?.Invoke();
        return eot;
    }

    private static bool Match(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
