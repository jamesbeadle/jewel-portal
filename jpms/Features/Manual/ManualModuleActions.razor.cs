
namespace Jewel.JPMS.Features.Manual;

/// <summary>The module's moves, by status and by who is signed in: edit and send for review, approve or return, revise, retire.</summary>
public partial class ManualModuleActions
{
    [Inject] private ICommandSender Commands { get; set; } = default!;

    [Parameter, EditorRequired] public ManualModule Module { get; set; } = default!;
    [Parameter] public bool CanManage { get; set; }
    [Parameter] public bool CanApprove { get; set; }
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private bool isBusy;
    private bool isReturning;
    private bool isRetiring;
    private string reason = "";
    private string? problem;

    private async Task RunAsync(ICommand<ManualModule> command)
    {
        isBusy = true;
        problem = null;
        try
        {
            await Commands.SendAsync(command, CancellationToken.None);
            isRetiring = false;
            await OnChanged.InvokeAsync();
        }
        catch (CommandFailedException refusal) { problem = refusal.Message; }
        finally { isBusy = false; }
    }

    private async Task ReturnAsync()
    {
        await RunAsync(new ReturnManualModuleToDraft(Module.ManualModuleId, reason));
        if (problem is not null) return;
        isReturning = false;
        reason = "";
    }
}
