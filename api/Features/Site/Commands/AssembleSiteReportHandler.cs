using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AssembleSiteReportHandler : ICommandHandler<AssembleSiteReport, SiteReport>
{
    private readonly JpmsContext context;
    public AssembleSiteReportHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteReport> HandleAsync(AssembleSiteReport command, CancellationToken cancellationToken)
    {
        var entity = new SiteReportEntity
        {
            SiteReportId = SiteIdentifierFactory.NextSiteReportId(),
            ProjectId = command.ProjectId,
            PeriodEnd = command.PeriodEnd,
            Narrative = command.Narrative,
            AttendanceDays = command.AttendanceDays,
            OpenSnags = command.OpenSnags,
            ProgressPercent = command.ProgressPercent,
            IsIssued = false
        };
        context.SiteReports.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
