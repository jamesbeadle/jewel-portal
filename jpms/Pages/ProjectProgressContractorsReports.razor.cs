using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Features.Progress.ContractorsReports;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgressContractorsReports
{
    [Parameter] public string ProjectId { get; set; } = "";

    private IReadOnlyList<ContractorsReport>? reports;
    private bool isOpening;
    private bool isBusy;
    private string? error;
    private DateOnly weekEnding = ContractorsReportWeeks.LastThursday(DateOnly.FromDateTime(DateTime.Today));
    private int? number;

    private IReadOnlyList<ContractorsReport> Reports => reports ?? Array.Empty<ContractorsReport>();

    private bool CanRead => Auth.IsSignedIn;

    // Mirrors ProjectProgress.CanContribute — the people who assemble progress reports.
    private bool CanContribute =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    protected override async Task OnInitializedAsync()
    {
        try { reports = await Queries.AskAsync(new ListContractorsReports(ProjectId), CancellationToken.None); }
        catch (Exception ex) { error = ex.Message; reports = Array.Empty<ContractorsReport>(); }
    }

    private string EditorPath(ContractorsReport report) =>
        $"/projects/{ProjectId}/progress/contractors-reports/{report.ContractorsReportId}";

    private async Task OpenAsync()
    {
        if (weekEnding.DayOfWeek != DayOfWeek.Thursday) { error = "Choose the Thursday the week ends on."; return; }
        error = null;
        isBusy = true;
        try
        {
            var report = await Commands.SendAsync(new CreateContractorsReport(ProjectId, weekEnding, number), CancellationToken.None);
            Nav.NavigateTo(EditorPath(report));
        }
        catch (Exception ex) { error = ex.Message; }
        finally { isBusy = false; }
    }
}
