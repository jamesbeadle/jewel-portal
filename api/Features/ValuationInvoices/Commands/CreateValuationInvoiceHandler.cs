using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.ValuationInvoices.Commands;

public sealed class CreateValuationInvoiceHandler : ICommandHandler<CreateValuationInvoice, ValuationInvoice>
{
    private readonly JpmsContext context;
    public CreateValuationInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<ValuationInvoice> HandleAsync(CreateValuationInvoice command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var nextNumber = (await context.ValuationInvoices
            .Where(call => call.ProjectId == command.ProjectId)
            .MaxAsync(call => (int?)call.Number, cancellationToken) ?? 0) + 1;

        var entity = new ValuationInvoiceEntity
        {
            ValuationInvoiceId = ValuationInvoicesIdentifierFactory.NextValuationInvoiceId(),
            ProjectId = command.ProjectId,
            ValuationClaimId = command.ValuationClaimId,
            Number = nextNumber,
            Reference = ValuationInvoicesIdentifierFactory.Reference(nextNumber),
            PeriodMonth = command.PeriodMonth,
            Amount = command.Amount,
            AmountPaid = 0m,
            Status = (int)ValuationInvoiceStatus.Raised,
            RaisedAt = DateTimeOffset.UtcNow
        };

        context.ValuationInvoices.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
