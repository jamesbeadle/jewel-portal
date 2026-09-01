using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgramme
{
    // ----- Gantt geometry -----

    private (DateTimeOffset Start, DateTimeOffset End) GanttRange
    {
        get
        {
            var starts = Tasks.Select(t => t.PlannedStart).Concat(BaselineTasks.Select(b => b.PlannedStart)).ToList();
            var ends = Tasks.Select(t => t.PlannedEnd).Concat(BaselineTasks.Select(b => b.PlannedEnd)).ToList();
            var start = starts.Min().AddDays(-3);
            var end = ends.Max().AddDays(3);
            if (end <= start) end = start.AddDays(1);
            return (start, end);
        }
    }

    private static string Pct(double value) =>
        value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "%";

    private string BarStyle(DateTimeOffset start, DateTimeOffset end)
    {
        var (rangeStart, rangeEnd) = GanttRange;
        var total = (rangeEnd - rangeStart).TotalDays;
        var left = Math.Max((start - rangeStart).TotalDays / total * 100, 0);
        var width = Math.Max((end - start).TotalDays / total * 100, 0.5);
        return $"left:{Pct(left)};width:{Pct(Math.Min(width, 100 - left))}";
    }

    private string? TodayStyle
    {
        get
        {
            var (rangeStart, rangeEnd) = GanttRange;
            var today = DateTimeOffset.UtcNow;
            if (today < rangeStart || today > rangeEnd) return null;
            return $"left:{Pct((today - rangeStart).TotalDays / (rangeEnd - rangeStart).TotalDays * 100)}";
        }
    }

    private IEnumerable<(string Label, string Style)> MonthMarks
    {
        get
        {
            var (rangeStart, rangeEnd) = GanttRange;
            var total = (rangeEnd - rangeStart).TotalDays;
            var month = new DateTimeOffset(rangeStart.Year, rangeStart.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1);
            while (month < rangeEnd)
            {
                yield return (month.ToString("MMM yy"), $"left:{Pct((month - rangeStart).TotalDays / total * 100)}");
                month = month.AddMonths(1);
            }
        }
    }

    private ProgrammeBaselineTask? BaselineTaskFor(string programmeTaskId) =>
        BaselineTasks.FirstOrDefault(b => b.ProgrammeTaskId == programmeTaskId);

    private int SlipDaysFor(string programmeTaskId) =>
        Movement.DelayEvents.FirstOrDefault(e => e.ProgrammeTaskId == programmeTaskId)?.SlipDays ?? 0;

    private IEnumerable<ProgrammeTaskLink> PredecessorsOf(string programmeTaskId) =>
        Links.Where(l => l.SuccessorTaskId == programmeTaskId);

    private string TaskTitle(string programmeTaskId) =>
        Tasks.FirstOrDefault(t => t.ProgrammeTaskId == programmeTaskId)?.Title ?? "(removed task)";

    private async Task LoadLadsAsync()
    {
        try
        {
            lads = await Queries.AskAsync(new ListLadClaimsForProject(ProjectId), CancellationToken.None);
            ladsFailed = false;
        }
        catch
        {
            lads = Array.Empty<LadClaim>();
            ladsFailed = true;
            claimsError = "Couldn't load the LADs claims. Please try again.";
        }
        finally
        {
            ladsLoaded = true;
        }
    }

    private async Task LoadEmailsAsync()
    {
        emailsError = null;
        try
        {
            emails = await Queries.AskAsync(new ListSchedulingEmails(ProjectId), CancellationToken.None);
        }
        catch
        {
            emails = Array.Empty<MailboxMessage>();
            emailsError = "Couldn't load programme emails. Please try again.";
        }
        finally
        {
            emailsLoaded = true;
        }
    }

}
