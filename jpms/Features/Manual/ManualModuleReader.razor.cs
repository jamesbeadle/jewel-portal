
namespace Jewel.JPMS.Features.Manual;

/// <summary>One published module as a reader sees it, with their acknowledgement of this version.</summary>
public partial class ManualModuleReader
{
    [Inject] private ICommandSender Commands { get; set; } = default!;

    [Parameter, EditorRequired] public ManualPublishedModule Module { get; set; } = default!;
    [Parameter] public bool CanAcknowledge { get; set; }
    [Parameter] public string SuggestedName { get; set; } = "";
    [Parameter] public EventCallback OnAcknowledged { get; set; }

    private string typedName = "";
    private bool isSaving;
    private string? problem;

    protected override void OnParametersSet()
    {
        if (typedName.Length == 0) typedName = SuggestedName;
    }

    private async Task AcknowledgeAsync()
    {
        isSaving = true;
        problem = null;
        try
        {
            await Commands.SendAsync(new AcknowledgeManualModule(Module.ManualModuleId, typedName.Trim()), CancellationToken.None);
            await OnAcknowledged.InvokeAsync();
        }
        catch (CommandFailedException refusal) { problem = refusal.Message; }
        finally { isSaving = false; }
    }
}
