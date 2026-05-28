using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordSiteVisitNotesHandler
    : ICommandHandler<RecordSiteVisitNotes, SiteVisit>
{
    private readonly JpmsContext context;

    public RecordSiteVisitNotesHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteVisit> HandleAsync(RecordSiteVisitNotes command, CancellationToken cancellationToken)
    {
        var entity = await context.SiteVisits.FindAsync(new object[] { command.SiteVisitId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Site visit {command.SiteVisitId} not found.");

        entity.Notes = command.Notes;
        entity.PhotoCount = command.PhotoCount;
        entity.IsComplete = command.IsComplete;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
