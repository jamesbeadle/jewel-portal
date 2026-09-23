using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Features.Forms.Office;

namespace Jewel.JPMS.Pages;

/// <summary>
/// The right to work register (the dashboard's Right to work tab): the checker's record, which is what
/// gives the statutory excuse, kept behind its own readers. Each row says whether the check is cleared,
/// not finished or a do-not-start, and what is missing; Record a check is the dashboard's numbered form.
/// </summary>
public partial class FormsRightToWork
{
    private const string Explainer = "Done properly there is no penalty at all; done badly it is up to £60,000 per worker. "
        + "A limited company subcontractor supplying their own staff is their responsibility, not ours - record it as such and move on. "
        + "The Right to Work Check form collects; it is not the check. The check is the original passport seen with them present, "
        + "or their share code run online, or an IDSP report.";

    private readonly FormWrite write = new();
    private readonly DateOnly today = DateOnly.FromDateTime(DateTime.Today);
    private IReadOnlyList<RightToWorkCheck>? checks;
    private bool dataFailed;
    private string filter = RightToWorkStanding.Current;
    private RightToWorkCheckDraft? editing;
    private RightToWorkCheck? confirming;

    private IReadOnlyList<RightToWorkCheck> Rows => RightToWorkStanding.Apply(checks ?? Array.Empty<RightToWorkCheck>(), filter);

    private IReadOnlyList<TabItem> Chips => RightToWorkStanding.Chips(checks);

    private string? NeedsALook => checks is null ? null : RightToWorkStanding.NeedsALook(checks, today);

    private string EmptyMessage => dataFailed ? "Couldn't load the register — refresh to try again." : "Nothing recorded yet. Use Record a check.";

    protected override async Task OnInitializedAsync()
    {
        var mayRead = await Entry.MayReadAsync(FormRoleSets.RightToWorkReaders);
        if (mayRead) await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try { checks = await Queries.AskAsync(new ListRightToWorkChecks(), CancellationToken.None); }
        catch { dataFailed = true; }
    }

    private void RecordNew() =>
        editing = new RightToWorkCheckDraft { CheckedByName = Auth.CurrentUser?.DisplayName ?? "", CheckedOn = FormDates.Write(today) };

    private void Correct(RightToWorkCheck check) =>
        editing = RightToWorkCheckDraft.Of(check.Details, check.RightToWorkCheckId, check.EngagementEndedOn);

    private async Task ClosedAsync()
    {
        editing = null;
        await LoadAsync();
    }

    private async Task ConfirmAsync()
    {
        var check = confirming!;
        var isSent = await write.RunAsync(() => Commands.SendAsync(new SendRightToWorkConfirmation(check.RightToWorkCheckId), CancellationToken.None));
        confirming = null;
        if (isSent) await LoadAsync();
    }
}
