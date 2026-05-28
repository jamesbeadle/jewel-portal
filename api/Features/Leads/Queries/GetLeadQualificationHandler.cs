using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Queries;

public sealed class GetLeadQualificationHandler
    : IQueryHandler<GetLeadQualification, QualificationAssessment?>
{
    private readonly JpmsContext context;

    public GetLeadQualificationHandler(JpmsContext context) { this.context = context; }

    public async Task<QualificationAssessment?> HandleAsync(
        GetLeadQualification query, CancellationToken cancellationToken)
    {
        var entity = await context.QualificationAssessments.FindAsync(new object[] { query.LeadId }, cancellationToken);
        return entity?.ToModel();
    }
}
