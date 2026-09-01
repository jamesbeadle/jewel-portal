using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

// Thrown when directory records still carry the trade; the endpoint answers 409 so the dialog
// shows the message inline rather than raising the error toast.
public sealed class DeleteTradeRefusedException : Exception
{
    public DeleteTradeRefusedException(string message) : base(message) { }
}

public sealed class DeleteTradeHandler : ICommandHandler<DeleteTrade, Acknowledgement>
{
    private readonly JpmsContext context;

    public DeleteTradeHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteTrade command, CancellationToken cancellationToken)
    {
        var entity = await context.Trades.FindAsync(new object[] { command.TradeId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Trade {command.TradeId} not found.");

        // Blocked while in use: deleting would strip the trade from every record carrying it, and
        // bid-package invite filters run on those links. Reassign the records first (the count is
        // the admin page's own usage figure). Bid packages snapshot the trade NAME and never block.
        var inUseBy = await context.SubcontractorTrades
            .CountAsync(link => link.TradeId == command.TradeId, cancellationToken);
        if (inUseBy > 0)
            throw new DeleteTradeRefusedException(
                $"“{entity.Name}” is on {inUseBy} directory record{(inUseBy == 1 ? "" : "s")} — reassign {(inUseBy == 1 ? "it" : "them")} first, then delete the trade.");

        context.Trades.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.TradeId);
    }
}
