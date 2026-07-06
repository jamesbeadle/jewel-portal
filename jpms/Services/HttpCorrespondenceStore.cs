using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCorrespondenceStore : ICorrespondenceStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpCorrespondenceStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<PartyContact>> ListPartyContactsAsync(PartyKind kind, string partyId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListPartyContacts(kind, partyId), cancellationToken);

    public Task<PartyContact> UpsertPartyContactAsync(UpsertPartyContact command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task RemovePartyContactAsync(PartyKind kind, string partyId, string partyContactId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemovePartyContact(kind, partyId, partyContactId), cancellationToken);

    public Task<IReadOnlyList<ProjectContact>> ListProjectContactsAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListProjectContacts(projectId), cancellationToken);

    public Task<ProjectContact> UpsertProjectContactAsync(UpsertProjectContact command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task RemoveProjectContactAsync(string projectId, string contactId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new RemoveProjectContact(projectId, contactId), cancellationToken);

    public Task<RequestRecipientSet> ResolveRequestRecipientsAsync(string requestId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ResolveRequestRecipients(requestId), cancellationToken);
}
