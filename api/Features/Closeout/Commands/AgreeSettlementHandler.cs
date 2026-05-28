using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class AgreeSettlementHandler : ICommandHandler<AgreeSettlement, SettlementRecord>
{
    private readonly JpmsContext context;
    public AgreeSettlementHandler(JpmsContext context) { this.context = context; }

    public async Task<SettlementRecord> HandleAsync(AgreeSettlement command, CancellationToken cancellationToken)
    {
        var existing = await context.SettlementRecords.FirstOrDefaultAsync(s => s.ProjectId == command.ProjectId, cancellationToken);
        var agreedAt = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            var entity = new SettlementRecordEntity
            {
                SettlementRecordId = CloseoutIdentifierFactory.NextSettlementRecordId(),
                ProjectId = command.ProjectId,
                FinalContractValue = command.FinalContractValue,
                FinalCost = command.FinalCost,
                FinalMargin = command.FinalMargin,
                AgreedAt = agreedAt,
                IsClientSigned = command.IsClientSigned
            };
            context.SettlementRecords.Add(entity);
            await context.SaveChangesAsync(cancellationToken);
            return entity.ToModel();
        }

        existing.FinalContractValue = command.FinalContractValue;
        existing.FinalCost = command.FinalCost;
        existing.FinalMargin = command.FinalMargin;
        existing.AgreedAt = agreedAt;
        existing.IsClientSigned = command.IsClientSigned;
        await context.SaveChangesAsync(cancellationToken);
        return existing.ToModel();
    }
}
