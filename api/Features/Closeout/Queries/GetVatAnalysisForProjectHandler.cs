using Jewel.JPMS.Contracts.Closeout;

namespace Jewel.JPMS.Api.Features.Closeout.Queries;

public sealed class GetVatAnalysisForProjectHandler : IQueryHandler<GetVatAnalysisForProject, VatAnalysis?>
{
    private readonly JpmsContext context;
    public GetVatAnalysisForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<VatAnalysis?> HandleAsync(GetVatAnalysisForProject query, CancellationToken cancellationToken)
    {
        var entity = await context.VatAnalyses.AsNoTracking().FirstOrDefaultAsync(v => v.ProjectId == query.ProjectId, cancellationToken);
        return entity?.ToModel();
    }
}
