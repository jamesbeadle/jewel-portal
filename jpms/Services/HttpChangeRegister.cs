using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Changes;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpChangeRegister : IChangeRegister
{
    private readonly ChangesReadModel readModel;
    private readonly ICommandSender commands;

    public HttpChangeRegister(ChangesReadModel readModel, ICommandSender commands)
    {
        this.readModel = readModel;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<ChangeRecord> ForProject(string projectId)
    {
        if (readModel.Current(projectId).Count == 0) _ = readModel.RefreshAsync(projectId, CancellationToken.None);
        return readModel.Current(projectId);
    }

    public IReadOnlyList<ChangeRecord> ForProject(string projectId, ChangeKind kind) =>
        ForProject(projectId).Where(record => record.Kind == kind).ToList().AsReadOnly();

    public ChangeRecord? Find(string changeRecordId) => null;

    public ChangeRecord Upsert(ChangeRecord record)
    {
        if (string.IsNullOrEmpty(record.ChangeRecordId))
            _ = RaiseAsync(record);
        else
            _ = UpdateAsync(record);
        return record;
    }

    private async Task RaiseAsync(ChangeRecord record)
    {
        await commands.SendAsync(new RaiseChange(record.ProjectId, record.Kind, record.Reference, record.Title, record.Description, record.Value, record.RaisedByEmail), CancellationToken.None);
        await readModel.RefreshAsync(record.ProjectId, CancellationToken.None);
    }

    private async Task UpdateAsync(ChangeRecord record)
    {
        await commands.SendAsync(new UpdateChangeDetails(record.ChangeRecordId, record.Reference, record.Title, record.Description, record.Status, record.Value, record.ResponseText, record.RespondedByEmail, record.ImpliesVariation), CancellationToken.None);
        await readModel.RefreshAsync(record.ProjectId, CancellationToken.None);
    }
}
