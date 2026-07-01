using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.CashCalls;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.CashCalls.Commands;

/// <summary>
/// Records the client's payment against a cash call and rolls the amount received into the
/// project-level CashCallTotal in the same transaction.
/// </summary>
public sealed class RecordCashCallReceiptHandler : ICommandHandler<RecordCashCallReceipt, CashCall>
{
    private readonly JpmsContext context;
    public RecordCashCallReceiptHandler(JpmsContext context) { this.context = context; }

    public async Task<CashCall> HandleAsync(RecordCashCallReceipt command, CancellationToken cancellationToken)
    {
        var entity = await context.CashCalls.FindAsync(new object[] { command.CashCallId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Cash call {command.CashCallId} not found.");
        if (entity.Status == (int)CashCallStatus.Received)
            throw new InvalidOperationException("This cash call has already been received.");

        var project = await context.Projects.FindAsync(new object[] { entity.ProjectId }, cancellationToken);
        if (project is null) throw new InvalidOperationException($"Project {entity.ProjectId} not found.");

        entity.AmountReceived = command.AmountReceived;
        entity.Status = (int)CashCallStatus.Received;
        entity.ReceivedAt = DateTimeOffset.UtcNow;

        project.CashCallTotal += command.AmountReceived;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
