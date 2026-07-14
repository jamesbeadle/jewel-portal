using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Mobilisation;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpMobilisationStore : IMobilisationStore
{
    private readonly MobilisationChecklistReadModel readModel;
    private readonly ICommandSender commands;

    // Projects whose checklist has been read — lets ToggleItem (id-only signature) resolve
    // which project an item belongs to from the cached checklists.
    private readonly HashSet<string> requested = new();

    public HttpMobilisationStore(MobilisationChecklistReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public MobilisationChecklist For(string projectId)
    {
        requested.Add(projectId);
        if (readModel.Current(projectId) is null) _ = readModel.RefreshAsync(projectId, CancellationToken.None);
        return readModel.Current(projectId) ?? new MobilisationChecklist(projectId, Array.Empty<MobilisationItem>());
    }

    public void ToggleItem(string mobilisationItemId)
    {
        // Search every cached checklist for the item (the old Current("") lookup only ever
        // searched a project with an empty id, so toggles silently did nothing).
        foreach (var projectId in requested)
        {
            var item = readModel.Current(projectId)?.Items.FirstOrDefault(candidate =>
                string.Equals(candidate.MobilisationItemId, mobilisationItemId, StringComparison.OrdinalIgnoreCase));
            if (item is null) continue;
            _ = ToggleAsync(projectId, item);
            return;
        }
    }

    private async Task ToggleAsync(string projectId, MobilisationItem item)
    {
        // Await the command, then re-pull the checklist so the ticked state shows without a
        // manual reload.
        await commands.SendAsync(
            new UpdateMobilisationChecklistItem(item.MobilisationItemId, item.Description, item.OwnerEmail, !item.IsComplete),
            CancellationToken.None);
        await readModel.RefreshAsync(projectId, CancellationToken.None);
    }
}
