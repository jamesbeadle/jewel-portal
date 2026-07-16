using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Queries;

// Everything the Programme tab's programme view needs in one round trip: live tasks, dependency
// links, and the latest baseline with its task snapshots (movement is computed from the pair via
// ProgrammeMovementCalculator, client- or agent-side).
public sealed class GetProgrammeDetailHandler : IQueryHandler<GetProgrammeDetail, ProgrammeDetail>
{
    private readonly JpmsContext context;
    public GetProgrammeDetailHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeDetail> HandleAsync(GetProgrammeDetail query, CancellationToken cancellationToken)
    {
        var tasks = await context.ProgrammeTasks
            .Where(task => task.ProjectId == query.ProjectId)
            .OrderBy(task => task.PlannedStart)
            .ToListAsync(cancellationToken);

        var links = await context.ProgrammeTaskLinks
            .Where(link => link.ProjectId == query.ProjectId)
            .ToListAsync(cancellationToken);

        var baselines = await context.ProgrammeBaselines
            .Where(b => b.ProjectId == query.ProjectId)
            .OrderByDescending(b => b.TakenAt)
            .ToListAsync(cancellationToken);

        var baseline = baselines.FirstOrDefault();

        var baselineTasks = baseline is null
            ? new List<Data.Entities.ProgrammeBaselineTaskEntity>()
            : await context.ProgrammeBaselineTasks
                .Where(t => t.ProgrammeBaselineId == baseline.ProgrammeBaselineId)
                .ToListAsync(cancellationToken);

        return new ProgrammeDetail(
            tasks.Select(t => t.ToModel()).ToList().AsReadOnly(),
            links.Select(l => l.ToModel()).ToList().AsReadOnly(),
            baseline?.ToModel(),
            baselineTasks.Select(t => t.ToModel()).ToList().AsReadOnly(),
            baselines.Select(b => b.ToModel()).ToList().AsReadOnly());
    }
}
