using Jewel.JPMS.Contracts.Registers;
using Jewel.JPMS.Features.Forms.Office;
using Jewel.JPMS.Features.Registers;
using Jewel.JPMS.Features.Registers.Policies;

namespace Jewel.JPMS.Pages;

/// <summary>
/// Policies &amp; sign-off: everyone's own queue to sign, and — for the register's readers — every
/// published revision with who has and hasn't signed it. The register's managers publish; the forms
/// office sends a Policy sign-off link to anyone without a login and chases the outstanding.
/// </summary>
public partial class Policies
{
    [Inject] private SessionService Session { get; set; } = default!;
    [Inject] private PolicyDocumentsReadModel PolicyDocs { get; set; } = default!;
    [Inject] private ICommandSender Commands { get; set; } = default!;

    private PublishedPoliciesPanel? published;
    private bool isPublishing;
    private PolicyDocument? sendingFor;
    private string? chasingId;
    private SentLinkOutcome? sent;
    private string? note;

    private bool CanPublish => Session.ActiveRole is { } role && RegisterRoleSets.ManageRegisters.Includes(role);

    private bool CanSend => Session.ActiveRole is { } role && FormRoleSets.Office.Includes(role);

    private bool CanReadTheRegister => Session.ActiveRole is { } role && RegisterRoleSets.PolicyReaders.Includes(role);

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        PolicyDocs.OnChanged += StateHasChanged;
        if (CanReadTheRegister) await RefreshAsync();
    }

    public void Dispose() => PolicyDocs.OnChanged -= StateHasChanged;

    private async Task RefreshAsync()
    {
        try { await PolicyDocs.RefreshAsync(CancellationToken.None); }
        catch (Exception) { note = "Couldn't load the published documents — reload to try again."; }
    }

    private async Task PublishedAsync(string? fileProblem)
    {
        isPublishing = false;
        note = fileProblem;
        await RefreshAsync();
    }

    private async Task LinkSentAsync(SentLinkOutcome outcome)
    {
        sendingFor = null;
        sent = outcome;
        await RefreshAsync();
        if (published is not null) await published.ReloadOpenAsync();
    }

    private async Task ChaseAsync(PolicySignOff row)
    {
        chasingId = row.PolicySignOffId;
        try
        {
            var chased = await Commands.SendAsync(new ChasePolicySignOff(row.PolicySignOffId), CancellationToken.None);
            await LinkSentAsync(SentLinkOutcome.Of(chased));
        }
        catch (CommandFailedException refusal) { note = refusal.Message; }
        finally { chasingId = null; }
    }
}
