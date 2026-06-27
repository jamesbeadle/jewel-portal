using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpIntakeQueue : IIntakeQueue
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    public HttpIntakeQueue(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
    }

    public Task<IReadOnlyList<IntakeEmail>> ListOpenAsync(CancellationToken cancellationToken = default) =>
        queries.AskAsync(new ListOpenIntake(), cancellationToken);

    public Task<IntakeEmailDetail> GetDetailAsync(string intakeId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new GetIntakeEmailDetail(intakeId), cancellationToken);

    public Task<RequestSuggestion> SuggestAsync(string intakeId, CancellationToken cancellationToken = default) =>
        queries.AskAsync(new SuggestRequestFromIntake(intakeId), cancellationToken);

    public Task<IntakeEmail> ClaimAsync(string intakeId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new ClaimIntakeEmail(intakeId), cancellationToken);

    public Task<IntakeEmail> DiscardAsync(string intakeId, string? notes, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new DiscardIntakeEmail(intakeId, notes), cancellationToken);

    public Task<IntakeEmail> LinkAsync(string intakeId, string requestId, CancellationToken cancellationToken = default) =>
        commands.SendAsync(new LinkIntakeToRequest(intakeId, requestId), cancellationToken);

    public Task<Request> CreateRequestAsync(CreateRequestFromIntake command, CancellationToken cancellationToken = default) =>
        commands.SendAsync(command, cancellationToken);
}
