using Jewel.JPMS.Contracts.Progress;
using Jewel.JPMS.Features.Progress.ContractorsReports;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgressContractorsReport
{
    [Parameter] public string ProjectId { get; set; } = "";
    [Parameter] public string ReportId { get; set; } = "";

    private ContractorsReportView? view;
    private ContractorsReportDraft? draft;
    private bool isBusy;
    private bool deleting;
    private bool saved;
    private string? error;

    private bool CanRead => Auth.IsSignedIn;

    // Mirrors ProjectProgress.CanContribute — the people who assemble progress reports.
    private bool CanContribute =>
        Auth.CurrentRoles.Any(role => role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager);

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            view = await Queries.AskAsync(new GetContractorsReport(ReportId), CancellationToken.None);
            if (view is null) { error = "This Contractor's Report no longer exists."; return; }
            draft = ContractorsReportDraft.From(view.Report);
        }
        catch (Exception ex) { error = ex.Message; }
    }

    private static string DownloadTitle(ContractorsReportView report, string format) => report.Document.CanBeBuilt
        ? $"Built from the register on every download as {format}"
        : "Refused until the findings above are fixed and saved";

    private async Task SaveAsync()
    {
        if (draft is null) return;
        await RunAsync(async () =>
        {
            await Commands.SendAsync(draft.ToCommand(ReportId), CancellationToken.None);
            await LoadAsync();
            saved = true;
        });
    }

    private async Task DeleteAsync()
    {
        deleting = false;
        await RunAsync(async () =>
        {
            await Commands.SendAsync(new DeleteContractorsReport(ReportId), CancellationToken.None);
            Nav.NavigateTo($"/projects/{ProjectId}/progress/contractors-reports");
        });
    }

    private async Task RunAsync(Func<Task> work)
    {
        error = null;
        saved = false;
        isBusy = true;
        try { await work(); }
        catch (Exception ex) { error = ex.Message; }
        finally { isBusy = false; }
    }
}
