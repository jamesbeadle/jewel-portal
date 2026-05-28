using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Mobilisation;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Mobilisation.Queries;

public sealed class GetMobilisationChecklistForProjectHandler
    : IQueryHandler<GetMobilisationChecklistForProject, MobilisationChecklist>
{
    private readonly JpmsContext context;
    public GetMobilisationChecklistForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<MobilisationChecklist> HandleAsync(GetMobilisationChecklistForProject query, CancellationToken cancellationToken)
    {
        var entities = await context.MobilisationItems.Where(item => item.ProjectId == query.ProjectId).ToListAsync(cancellationToken);
        var items = entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
        return new MobilisationChecklist(query.ProjectId, items);
    }
}
