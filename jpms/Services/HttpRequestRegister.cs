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
            _ = RaiseRecordAsync(record);
        else
            _ = UpdateRecordAsync(record);
        return record;
    }

    public Task<Request?> GetAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetRequestById(requestId), cancellationToken);

    public async Task<Request> RaiseAsync(RaiseRequest command, CancellationToken cancellationToken = default)
    {
        var raised = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAsync(command.ProjectId, cancellationToken);
        return raised;
    }

    public async Task<Request> UpdateAsync(UpdateRequestDetails command, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(command, cancellationToken);
        await readModel.RefreshAsync(updated.ProjectId, cancellationToken);
        return updated;
    }

    public Task<IReadOnlyList<RequestMessage>> ListMessagesAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListRequestMessages(requestId), cancellationToken);

    public Task<RequestMessage> PostMessageAsync(PostRequestMessage command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public async Task DeleteAsync(string requestId, string projectId, CancellationToken cancellationToken = default)
    {
        await commands.SendAsync(new DeleteRequest(requestId), cancellationToken);
        await readModel.RefreshAsync(projectId, cancellationToken);
    }

    public async Task ReturnToTriageAsync(string requestId, string projectId, CancellationToken cancellationToken = default)
    {
        await commands.SendAsync(new ReturnRequestToTriage(requestId), cancellationToken);
        await readModel.RefreshAsync(projectId, cancellationToken);
    }

    public Task<IReadOnlyList<Request>> ListUnassignedAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListUnassignedRequests(), cancellationToken);

    public async Task<Request> PromoteToRfiAsync(string requestId, string projectId, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(new PromoteRequestToRfi(requestId), cancellationToken);
        await readModel.RefreshAsync(projectId, cancellationToken);
        return updated;
    }

    public async Task<Request> EnableRfqAsync(string requestId, string projectId, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(new EnableRfqOnRequest(requestId), cancellationToken);
        await readModel.RefreshAsync(projectId, cancellationToken);
        return updated;
    }

    public async Task<Request> LinkToClientAsync(string requestId, string? clientId, string projectId, CancellationToken cancellationToken = default)
    {
        var updated = await commands.SendAsync(new LinkRequestToClient(requestId, clientId), cancellationToken);
        await readModel.RefreshAsync(projectId, cancellationToken);
        return updated;
    }

    private async Task RaiseRecordAsync(Request record)
    {
        await commands.SendAsync(new RaiseRequest(record.ProjectId, record.Kind, record.Reference, record.Title, record.Description, record.Value, record.RaisedByEmail, record.RaisedTo, record.DrawingRef, record.ResponseDue, record.InternalNotes, record.ClientNotes), CancellationToken.None);
        await readModel.RefreshAsync(record.ProjectId, CancellationToken.None);
    }

    private async Task UpdateRecordAsync(Request record)
    {
        await commands.SendAsync(new UpdateRequestDetails(record.RequestId, record.Reference, record.Title, record.Description, record.Status, record.Value, record.ResponseText, record.RespondedByEmail, record.ImpliesVariation, record.RaisedTo, record.DrawingRef, record.ResponseDue, record.RelatedDrawingSpec, record.InternalNotes, record.ClientNotes), CancellationToken.None);
        await readModel.RefreshAsync(record.ProjectId, CancellationToken.None);
    }
}
