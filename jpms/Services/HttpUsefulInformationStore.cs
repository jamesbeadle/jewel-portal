using Jewel.JPMS.Contracts.UsefulInformation;

namespace Jewel.JPMS.Services;

public sealed class HttpUsefulInformationStore : IUsefulInformationStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpUsefulInformationStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<UsefulInformationNote>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListUsefulInformationForProject(projectId), cancellationToken);

    public Task<UsefulInformationNote> AddAsync(AddUsefulInformationNote command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<UsefulInformationNote> UpdateAsync(UpdateUsefulInformationNote command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);

    public Task<Acknowledgement> DeleteAsync(string usefulInformationNoteId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DeleteUsefulInformationNote(usefulInformationNoteId), cancellationToken);
}
