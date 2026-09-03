using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi.Commands;

// Re-files a KPI under another person and/or rewrites its note. Snapshot and reference stay.
public sealed class UpdateKpiEmailHandler : ICommandHandler<UpdateKpiEmail, KpiEmail>
{
    private readonly JpmsContext context;
    private readonly KpiPersonResolver people;
    public UpdateKpiEmailHandler(JpmsContext context, KpiPersonResolver people) { this.context = context; this.people = people; }

    public async Task<KpiEmail> HandleAsync(UpdateKpiEmail command, CancellationToken cancellationToken)
    {
        var entity = await context.KpiEmails.FirstOrDefaultAsync(row => row.KpiEmailId == command.KpiEmailId, cancellationToken)
            ?? throw new InvalidOperationException($"KPI '{command.KpiEmailId}' not found.");

        var person = await people.ResolveAsync(command.PersonId, command.PersonEmail, command.PersonName, cancellationToken);

        if (person.KpiPersonId != entity.PersonId)
        {
            var twin = await context.KpiEmails.AnyAsync(
                row => row.KpiEmailId != entity.KpiEmailId
                    && row.PersonId == person.KpiPersonId
                    && row.InternetMessageId == entity.InternetMessageId,
                cancellationToken);
            if (twin)
                throw new InvalidOperationException($"This email is already a KPI for {person.Name}.");
            entity.PersonId = person.KpiPersonId;
        }

        entity.Note = (command.Note ?? "").Trim();
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel(person);
    }
}
