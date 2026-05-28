using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Mobilisation;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpMobilisationStore : IMobilisationStore
{
    private readonly MobilisationChecklistReadModel readModel;
    private readonly ICommandSender commands;

    public HttpMobilisationStore(MobilisationChecklistReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public MobilisationChecklist For(string projectId)
    {
        if (readModel.Current(projectId) is null) _ = readModel.RefreshAsync(projectId, CancellationToken.None);
        return readModel.Current(projectId) ?? new MobilisationChecklist(projectId, Array.Empty<MobilisationItem>());
    }

    public void ToggleItem(string mobilisationItemId)
    {
        var item = readModel
            .Current("") // best-effort: we don't have projectId here, so search any cached project
            ?.Items.FirstOrDefault(item => string.Equals(item.MobilisationItemId, mobilisationItemId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        _ = commands.SendAsync(new UpdateMobilisationChecklistItem(item.MobilisationItemId, item.Description, item.OwnerEmail, !item.IsComplete), CancellationToken.None);
    }
}
