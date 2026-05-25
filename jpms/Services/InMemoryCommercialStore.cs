using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryCommercialStore : ICommercialStore
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";
    private readonly DateTimeOffset baseDate = DateTimeOffset.UtcNow.AddMonths(-3);

    private readonly List<ClaimPeriod> claimPeriods;
    private readonly List<Valuation> valuations;
    private readonly List<CostCodeBudget> budgets;
    private readonly List<Timesheet> timesheets;

    public InMemoryCommercialStore()
    {
        claimPeriods = CommercialSeed.ClaimPeriods(baseDate);
        valuations = CommercialSeed.Valuations(claimPeriods);
        budgets = CommercialSeed.Budgets();
        timesheets = CommercialSeed.Timesheets(NigelEmail);
    }

    public event Action? OnChange;

    public IReadOnlyList<ClaimPeriod> ClaimPeriodsFor(string projectId) =>
        claimPeriods.Where(c => Match(c.ProjectId, projectId))
                    .OrderBy(c => c.PeriodNumber).ToList().AsReadOnly();

    public IReadOnlyList<Valuation> ValuationsFor(string projectId) =>
        valuations.Where(v => Match(v.ProjectId, projectId))
                  .OrderByDescending(v => v.IssuedAt ?? DateTimeOffset.MinValue).ToList().AsReadOnly();

    public Valuation SaveValuation(Valuation valuation)
    {
        var existing = valuations.FirstOrDefault(v => v.ValuationId == valuation.ValuationId);
        if (existing is not null) valuations.Remove(existing);
        valuations.Add(valuation);
        OnChange?.Invoke();
        return valuation;
    }

    public IReadOnlyList<CostCodeBudget> BudgetsFor(string projectId) =>
        budgets.Where(b => Match(b.ProjectId, projectId))
               .OrderBy(b => b.CostCode).ToList().AsReadOnly();

    public IReadOnlyList<Timesheet> TimesheetsFor(string projectId) =>
        timesheets.Where(t => Match(t.ProjectId, projectId))
                  .OrderByDescending(t => t.WorkedOn).ToList().AsReadOnly();

    public Timesheet SaveTimesheet(Timesheet timesheet)
    {
        var existing = timesheets.FirstOrDefault(t => t.TimesheetId == timesheet.TimesheetId);
        if (existing is not null) timesheets.Remove(existing);
        timesheets.Add(timesheet);
        OnChange?.Invoke();
        return timesheet;
    }

    public Timesheet ApproveTimesheet(string timesheetId)
    {
        var existing = timesheets.First(t => t.TimesheetId == timesheetId);
        return SaveTimesheet(existing with { IsApproved = true });
    }

    public CashflowSnapshot LatestCashflow() => CommercialSeed.LatestCashflow();

    private static bool Match(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
