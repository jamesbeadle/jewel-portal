using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgressWhatsAppWeek
{
    [Parameter] public string ProjectId { get; set; } = "";

    private DateOnly weekEnding = LastThursday(DateOnly.FromDateTime(DateTime.Today));
    private string chatText = "";
    private IBrowserFile? export;
    private WhatsAppWeekPreview? preview;
    private readonly HashSet<DateOnly> tickedDays = new();
    private WhatsAppWeekApplied? applied;
    private bool isBusy;
    private bool confirming;
    private string? error;

    // Mirrors ProjectProgress.CanContribute — the same people who may record progress there.
    private bool CanContribute =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    private bool CanPreview => !isBusy && (export is not null || !string.IsNullOrWhiteSpace(chatText));

    private string ConfirmMessage =>
        $"{tickedDays.Count} progress update{(tickedDays.Count == 1 ? "" : "s")} will be created on this project, " +
        "one per ticked day, with that day's photographs. Existing updates are untouched and nothing is sent to anyone.";

    private static DateOnly LastThursday(DateOnly today)
    {
        var day = today;
        while (day.DayOfWeek != DayOfWeek.Thursday) day = day.AddDays(-1);
        return day;
    }

    private void OnWeekEndingChanged(DateOnly value)
    {
        weekEnding = value;
        preview = null;
    }

    private void OnExportChosen(IBrowserFile? file)
    {
        export = file;
        preview = null;
    }

    private async Task PreviewAsync()
    {
        if (weekEnding.DayOfWeek != DayOfWeek.Thursday) { error = "Choose the Thursday the week ends on."; return; }
        await RunAsync(async () =>
        {
            preview = await Intake.PreviewAsync(ProjectId, weekEnding, chatText, export, CancellationToken.None);
            tickedDays.Clear();
            foreach (var day in preview.Days.Where(day => !day.HasExistingUpdate)) tickedDays.Add(day.Date);
        });
    }

    private async Task ApplyAsync()
    {
        confirming = false;
        await RunAsync(async () =>
        {
            applied = await Intake.ApplyAsync(ProjectId, weekEnding, chatText, export, tickedDays.OrderBy(day => day).ToList(), CancellationToken.None);
        });
    }

    private async Task RunAsync(Func<Task> work)
    {
        error = null;
        isBusy = true;
        try { await work(); }
        catch (Exception ex) { error = ex.Message; }
        finally { isBusy = false; }
    }
}
