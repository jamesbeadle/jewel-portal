using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.Commands;

/// <summary>Records a dated site note on the project's progress feed, with no photographs yet —
/// the connector's create_progress_update and the text-only days of the WhatsApp week intake.</summary>
public sealed class CreateProgressUpdateHandler
    : ICommandHandler<CreateProgressUpdate, ProgressUpdate>
{
    private readonly JpmsContext context;

    public CreateProgressUpdateHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgressUpdate> HandleAsync(CreateProgressUpdate command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(row => row.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var update = ProgressUpdateRows.New(
            ProgressIdentifierFactory.NextProgressUpdateId(), command.ProjectId, command.Title, command.Description,
            command.WorkDate, command.Weather, command.CreatedByEmail, DateTimeOffset.UtcNow);
        context.ProgressUpdates.Add(update);
        await context.SaveChangesAsync(cancellationToken);
        return update.ToModel(Array.Empty<ProgressPhoto>());
    }
}
