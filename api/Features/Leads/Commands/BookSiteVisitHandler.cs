using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class BookSiteVisitHandler
    : ICommandHandler<BookSiteVisit, SiteVisit>
{
    private readonly JpmsContext context;

    public BookSiteVisitHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteVisit> HandleAsync(BookSiteVisit command, CancellationToken cancellationToken)
    {
        var entity = new SiteVisitEntity
        {
            SiteVisitId = LeadIdentifierFactory.NextSiteVisitId(),
            LeadId = command.LeadId,
            ScheduledAt = command.ScheduledAt,
            AttendeeEmailsCsv = string.Join(",", command.AttendeeEmails),
            Notes = "",
            PhotoCount = 0,
            IsComplete = false
        };
        context.SiteVisits.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
