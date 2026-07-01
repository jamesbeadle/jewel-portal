using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class CreateCashCallHandler : ICommandHandler<CreateCashCall, CashCall>
{
    private readonly JpmsContext context;
    public CreateCashCallHandler(JpmsContext context) { this.context = context; }

    public async Task<CashCall> HandleAsync(CreateCashCall command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var nextNumber = (await context.CashCalls
            .Where(call => call.ProjectId == command.ProjectId)
            .MaxAsync(call => (int?)call.Number, cancellationToken) ?? 0) + 1;

        var entity = new CashCallEntity
        {
            CashCallId = CashCallsIdentifierFactory.NextCashCallId(),
            ProjectId = command.ProjectId,
            ValuationClaimId = command.ValuationClaimId,
            Number = nextNumber,
            Reference = CashCallsIdentifierFactory.Reference(nextNumber),
            PeriodMonth = command.PeriodMonth,
            AmountRequested = command.AmountRequested,
            AmountReceived = 0m,
            Status = (int)CashCallStatus.Requested,
            RequestedAt = DateTimeOffset.UtcNow
        };

        context.CashCalls.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
