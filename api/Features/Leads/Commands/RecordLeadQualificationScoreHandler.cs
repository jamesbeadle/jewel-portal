using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordLeadQualificationScoreHandler
    : ICommandHandler<RecordLeadQualificationScore, QualificationAssessment>
{
    private readonly JpmsContext context;

    public RecordLeadQualificationScoreHandler(JpmsContext context) { this.context = context; }

    public async Task<QualificationAssessment> HandleAsync(
        RecordLeadQualificationScore command, CancellationToken cancellationToken)
    {
        var existing = await context.QualificationAssessments.FindAsync(new object[] { command.LeadId }, cancellationToken);
        var assessedAt = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var entity = new QualificationAssessmentEntity
            {
                LeadId = command.LeadId,
                Score = command.Score,
                Notes = command.Notes,
                AssessedByEmail = command.AssessedByEmail,
                AssessedAt = assessedAt
            };
            context.QualificationAssessments.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            return entity.ToModel();
        }

        existing.Score = command.Score;
        existing.Notes = command.Notes;
        existing.AssessedByEmail = command.AssessedByEmail;
        existing.AssessedAt = assessedAt;
        await context.SaveChangesAsync(cancellationToken);
        return existing.ToModel();
    }
}
