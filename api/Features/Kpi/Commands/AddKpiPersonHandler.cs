using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi.Commands;

// Adds someone KPIs can be filed under: by portal email (their one row, found or created from
// the directory) or by name alone. Idempotent — an existing match is answered, never twinned.
public sealed class AddKpiPersonHandler : ICommandHandler<AddKpiPerson, KpiPerson>
{
    private readonly JpmsContext context;
    private readonly KpiPersonResolver people;
    public AddKpiPersonHandler(JpmsContext context, KpiPersonResolver people) { this.context = context; this.people = people; }

    public async Task<KpiPerson> HandleAsync(AddKpiPerson command, CancellationToken cancellationToken)
    {
        var person = !string.IsNullOrWhiteSpace(command.Email)
            ? await people.ForPortalUserAsync(command.Email.Trim(), cancellationToken)
            : await people.ForNameAsync(command.Name.Trim(), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        var count = await context.KpiEmails.CountAsync(row => row.PersonId == person.KpiPersonId, cancellationToken);
        return person.ToModel(count);
    }
}
