using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpRequestRegister : IRequestRegister
{
    private readonly RequestsReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpRequestRegister(RequestsReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Request> ForProject(string projectId)
    {
        if (readModel.Current(projectId).Count == 0) _ = readModel.RefreshAsync(projectId, CancellationToken.None);
        return readModel.Current(projectId);
    }

    public IReadOnlyList<Request> ForProject(string projectId, RequestType kind) =>
        ForProject(projectId).Where(record => record.Kind == kind).ToList().AsReadOnly();

    public Request? Find(string requestId) =>
        queries.AskAsync(new GetRequestById(requestId), CancellationToken.None).GetAwaiter().GetResult();

    public Request Upsert(Request record)
    {
        if (string.IsNullOrEmpty(record.RequestId))
            _ = RaiseAsync(record);
        else
            _ = UpdateAsync(record);
        return record;
    }

    private async Task RaiseAsync(Request record)
    {
        await commands.SendAsync(new RaiseRequest(record.ProjectId, record.Kind, record.Reference, record.Title, record.Description, record.Value, record.RaisedByEmail, record.RaisedTo, record.DrawingRef, record.ResponseDue, record.InternalNotes, record.ClientNotes), CancellationToken.None);
        await readModel.RefreshAsync(record.ProjectId, CancellationToken.None);
    }

    private async Task UpdateAsync(Request record)
    {
        await commands.SendAsync(new UpdateRequestDetails(record.RequestId, record.Reference, record.Title, record.Description, record.Status, record.Value, record.ResponseText, record.RespondedByEmail, record.ImpliesVariation, record.RaisedTo, record.DrawingRef, record.ResponseDue, record.RelatedDrawingSpec, record.InternalNotes, record.ClientNotes), CancellationToken.None);
        await readModel.RefreshAsync(record.ProjectId, CancellationToken.None);
    }
}
