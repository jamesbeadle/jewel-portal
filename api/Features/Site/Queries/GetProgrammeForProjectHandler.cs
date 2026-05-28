using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Queries;

public sealed class GetProgrammeForProjectHandler : IQueryHandler<GetProgrammeForProject, IReadOnlyList<ProgrammeTask>>
{
    private readonly JpmsContext context;
    public GetProgrammeForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProgrammeTask>> HandleAsync(GetProgrammeForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.ProgrammeTasks.Where(task => task.ProjectId == query.ProjectId).OrderBy(task => task.PlannedStart).ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
