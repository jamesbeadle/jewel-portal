using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class ApproveSiteReportHandler : ICommandHandler<ApproveSiteReport, SiteReport>
{
    private readonly JpmsContext context;
    public ApproveSiteReportHandler(JpmsContext context) { this.context = context; }

    public async Task<SiteReport> HandleAsync(ApproveSiteReport command, CancellationToken cancellationToken)
    {
        var entity = await context.SiteReports.FindAsync(new object[] { command.SiteReportId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Site report {command.SiteReportId} not found.");
        entity.IsIssued = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
