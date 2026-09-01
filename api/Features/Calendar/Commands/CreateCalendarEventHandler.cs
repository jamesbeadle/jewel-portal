using Jewel.JPMS.Contracts.Calendar;

namespace Jewel.JPMS.Api.Features.Calendar.Commands;

public sealed class CreateCalendarEventHandler : ICommandHandler<CreateCalendarEvent, CalendarEvent>
{
    private readonly JpmsContext context;
    private readonly CalendarEventRegister register;

    public CreateCalendarEventHandler(JpmsContext context, CalendarEventRegister register)
    {
        this.context = context;
        this.register = register;
    }

    public async Task<CalendarEvent> HandleAsync(CreateCalendarEvent command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        var entity = await register.RaiseAsync(command.ProjectId, command.Details, command.CreatedByEmail, cancellationToken);
        return entity.ToModel();
    }
}
