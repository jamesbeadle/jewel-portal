using Jewel.JPMS.Contracts.Lads;
using Jewel.JPMS.Contracts.Site;

namespace Jewel.JPMS.Pages;

public partial class ProjectProgramme
{
    [Parameter] public string ProjectId { get; set; } = "";

    private enum SubView { Programme, Claims, CriticalRfis, RelevantEvents }
    private SubView view = SubView.Programme;

    private enum ClaimForm { None, Nod, Eot, Lad }
    private ClaimForm openForm = ClaimForm.None;

    // Session checked and the user is signed in — not "the data is here". The heading and the
    // sub-tabs show straight away; each view holds its own panels until their sources land.
    private bool emailsLoaded;
    private string? emailsError;
    private IReadOnlyList<MailboxMessage> emails = Array.Empty<MailboxMessage>();

    // Claims state. NOD/EOT come from the request register (they ARE requests, of those kinds);
    // LADs claims come from their own store.
    private bool ladsLoaded;
    // Kept apart from claimsError, which also carries the raise/record failures: only a failed
    // *fetch* means the empty list is not an answer.
    private bool ladsFailed;
    private string? claimsError;
    private bool claimsBusy;
    private IReadOnlyList<LadClaim> lads = Array.Empty<LadClaim>();

    // Raise-NOD form.
    private string nodTitle = "";
    private string nodDescription = "";

    // Raise-EOT form.
    private string eotTitle = "";
    private string eotDescription = "";
    private string eotRelatedNodId = "";

    // Record-LADs form.
    private string ladTitle = "";
    private string ladDescription = "";
    private DateTime? ladPeriodFrom;
    private DateTime? ladPeriodTo;
    private int ladDaysClaimed;
    private decimal ladRatePerWeek;
    private decimal ladAmount;

    // The register backs the NODs, the EOTs and the critical-path RFIs alike: until it has landed
    // an empty list is indistinguishable from a project with no claims at all.
    private bool RequestsReady => RequestRegister.LoadedFor(ProjectId);

    private IReadOnlyList<Request> Nods => RequestRegister.ForProject(ProjectId, RequestType.NoticeOfDelay);
    private IReadOnlyList<Request> Eots => RequestRegister.ForProject(ProjectId, RequestType.ExtensionOfTime);
    private int ClaimsCount => Nods.Count + Eots.Count + (ladsLoaded ? lads.Count : 0);

    // RFIs tagged Critical Path (set on the RFI's detail page), open first then by reference —
    // outstanding programme blockers lead, answered ones stay below as the impact history. The
    // register is the same stale-while-revalidate store the claims read from, so the page-entry
    // Refresh in OnInitializedAsync keeps this subset current too.
    private IReadOnlyList<Request> CriticalRfis => RequestRegister
        .ForProject(ProjectId, RequestType.Rfi)
        .Where(r => r.CriticalPath)
        .OrderBy(r => r.Status is RequestStatus.Closed ? 1 : 0)
        .ThenBy(r => r.Reference, StringComparer.OrdinalIgnoreCase)
        .ToList();

    // The tab badge counts only the open ones — the number that still gates the programme.
    private int OpenCriticalRfiCount => CriticalRfis.Count(r => r.Status is not RequestStatus.Closed);

    // Programme state. The detail (tasks, links, latest baseline) comes down in one query; movement
    // is computed locally with the same calculator the API's Programme Agent uses, so the banner
    // here and the agent's delay analysis always agree.
    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        RequestRegister.OnChange += StateHasChanged;
        // Refresh on entry so NoD / EOT rows and count badges reflect changes made since the
        // register was first cached (stale-while-revalidate).
        RequestRegister.Refresh(ProjectId);
        // Load claims + relevant events in the background so the count badges are ready when the
        // tabs are opened; the programme workbench reads its own.
        await Task.WhenAll(LoadLadsAsync(), LoadEmailsAsync());
    }

    public void Dispose() => RequestRegister.OnChange -= StateHasChanged;

    private void SwitchView(SubView next) => view = next;

    private void ToggleForm(ClaimForm form)
    {
        openForm = openForm == form ? ClaimForm.None : form;
        claimsError = null;
    }

    // Pre-fills the Notice of Delay form from a detected delay event and jumps to the Claims view —
    // the human still reviews and raises it (the notice itself is never issued automatically).
    private void RaiseNodFromDelay(ProgrammeDelayEvent delayEvent)
    {
        nodTitle = $"Delay to {delayEvent.Title} — programme impact {delayEvent.SlipDays} day(s)";
        nodDescription =
            $"The programme task \"{delayEvent.Title}\" has moved against the baselined programme: " +
            $"planned completion {delayEvent.BaselineEnd.LocalDateTime:d MMM yyyy} now forecast {delayEvent.PlannedEnd.LocalDateTime:d MMM yyyy} " +
            $"({delayEvent.SlipDays} day(s) slippage{(delayEvent.DrivesCompletion ? ", driving overall project completion" : "")}). " +
            "Cause of delay and works affected: [complete before issuing].";
        view = SubView.Claims;
        openForm = ClaimForm.Nod;
    }

}
