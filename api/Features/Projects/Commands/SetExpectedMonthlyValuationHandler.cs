using Jewel.JPMS.Contracts.Projects;

namespace Jewel.JPMS.Api.Features.Projects.Commands;

public sealed class SetExpectedMonthlyValuationHandler
    : ICommandHandler<SetExpectedMonthlyValuation, Project>
{
    private readonly JpmsContext context;

    public SetExpectedMonthlyValuationHandler(JpmsContext context) { this.context = context; }

    public async Task<Project> HandleAsync(SetExpectedMonthlyValuation command, CancellationToken cancellationToken)
    {
        var entity = await context.Projects.FindAsync(new object[] { command.ProjectId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        // Whole pennies — it is a forecast assumption, not an invoice line.
        entity.ExpectedMonthlyValuation = command.ExpectedMonthlyValuation is { } value
            ? Math.Round(value, 2)
            : null;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
