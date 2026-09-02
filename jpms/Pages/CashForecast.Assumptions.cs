using static Jewel.JPMS.Features.Cashflow.CashflowDisplay;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Features.Commercial;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Projects;


namespace Jewel.JPMS.Pages;

public partial class CashForecast
{
    // ---- The FD's inline forecast assumptions (2026-08-13) --------------------------------
    // The two knobs behind the Future valuations row: when the next valuation lands, and
    // roughly how much the architect is expected to certify per month. Each saves through its
    // own command, then the project list is re-read so BuildForecast picks the stored values
    // up on the next render — the forecast, the reconciliation check and every other reader
    // of SelectedProjects see the same project. An empty (or zero) amount clears the view and
    // returns the project to the even spread.

    // A full date since 2026-08-17 (was a month picker): the day is the anchor the payment
    // lag counts from, so the forecast lands cash on payment dates rather than valuation
    // dates. Dates saved by the old month picker parse as the 1st — the engine's old anchor —
    // so nothing shifts until the FD sets a real valuation day.
    private async Task OnNextValuationChangedAsync(Project project, ChangeEventArgs args)
    {
        DateTimeOffset? next = DateTime.TryParseExact(
                args.Value?.ToString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var day)
            ? new DateTimeOffset(day, TimeSpan.Zero)
            : null;
        await SaveForecastAssumptionAsync(new SetNextValuationDate(project.ProjectId, next));
    }

    private async Task OnMonthlyValuationChangedAsync(Project project, ChangeEventArgs args)
    {
        decimal? monthly = decimal.TryParse(args.Value?.ToString(), out var value) && value > 0m
            ? Math.Round(value, 2)
            : null;
        await SaveForecastAssumptionAsync(new SetExpectedMonthlyValuation(project.ProjectId, monthly));
    }

    private async Task SaveForecastAssumptionAsync(ICommand<Project> command)
    {
        try
        {
            await Commands.SendAsync(command, CancellationToken.None);
            await Projects.RefreshAsync(CancellationToken.None);
        }
        catch
        {
            // HttpCommandSender / the read model have already raised the error toast with a
            // reference; the inputs simply keep showing the last-saved values.
        }
    }

    // What the combined statement's header names — mirroring ProjectMultiSelect's own toggle
    // label rules so the card and the filter always describe the selection in the same words.
    private string SelectionLabel
    {
        get
        {
            var all = Projects.Current ?? (IReadOnlyList<Project>)Array.Empty<Project>();
            var selected = SelectedProjects;
            if (selected.Count == 1) return $"{selected[0].Reference} · {selected[0].Name}";
            if (selected.Count == all.Count) return $"All projects ({selected.Count})";
            var live = all.Where(ProjectMultiSelect.IsLiveJob).ToList();
            if (selected.Count == live.Count
                && live.All(project => selectedIds.Contains(project.ProjectId, StringComparer.OrdinalIgnoreCase)))
                return $"Live jobs ({selected.Count})";
            return $"{selected.Count} of {all.Count} projects";
        }
    }


}
