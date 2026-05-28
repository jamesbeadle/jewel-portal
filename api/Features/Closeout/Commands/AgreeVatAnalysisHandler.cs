using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeVatAnalysisHandler : ICommandHandler<AgreeVatAnalysis, VatAnalysis>
{
    private readonly JpmsContext context;
    public AgreeVatAnalysisHandler(JpmsContext context) { this.context = context; }

    public async Task<VatAnalysis> HandleAsync(AgreeVatAnalysis command, CancellationToken cancellationToken)
    {
        var existing = await context.VatAnalyses.FirstOrDefaultAsync(v => v.ProjectId == command.ProjectId, cancellationToken);
        if (existing is null)
        {
            var entity = new VatAnalysisEntity
            {
                VatAnalysisId = CloseoutIdentifierFactory.NextVatAnalysisId(),
                ProjectId = command.ProjectId,
                ZeroRatedAmount = command.ZeroRatedAmount,
                StandardRatedAmount = command.StandardRatedAmount,
                Notes = command.Notes,
                IsClientConfirmed = command.IsClientConfirmed,
                IsArchitectConfirmed = command.IsArchitectConfirmed
            };
            context.VatAnalyses.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            return entity.ToModel();
        }

        existing.ZeroRatedAmount = command.ZeroRatedAmount;
        existing.StandardRatedAmount = command.StandardRatedAmount;
        existing.Notes = command.Notes;
        existing.IsClientConfirmed = command.IsClientConfirmed;
        existing.IsArchitectConfirmed = command.IsArchitectConfirmed;
        await context.SaveChangesAsync(cancellationToken);
        return existing.ToModel();
    }
}
