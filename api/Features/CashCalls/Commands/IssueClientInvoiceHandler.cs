using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

public sealed class IssueClientInvoiceHandler : ICommandHandler<IssueClientInvoice, CashCall>
{
    private readonly JpmsContext context;
    public IssueClientInvoiceHandler(JpmsContext context) { this.context = context; }

    public async Task<CashCall> HandleAsync(IssueClientInvoice command, CancellationToken cancellationToken)
    {
        var entity = await context.CashCalls.FindAsync(new object[] { command.CashCallId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Cash call {command.CashCallId} not found.");
        if (entity.Status == (int)CashCallStatus.Received)
            throw new InvalidOperationException("A received cash call cannot be re-invoiced.");

        entity.Status = (int)CashCallStatus.Invoiced;
        entity.InvoicedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
