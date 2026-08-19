using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

// Thrown when the new name is already held by a different trade; the endpoint answers 409 so the
// dialog shows the message inline rather than raising the error toast.
public sealed class RenameTradeRefusedException : Exception
{
    public RenameTradeRefusedException(string message) : base(message) { }
}

public sealed class RenameTradeHandler : ICommandHandler<RenameTrade, Trade>
{
    private readonly JpmsContext context;

    public RenameTradeHandler(JpmsContext context) { this.context = context; }

    public async Task<Trade> HandleAsync(RenameTrade command, CancellationToken cancellationToken)
    {
        var entity = await context.Trades.FindAsync(new object[] { command.TradeId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Trade {command.TradeId} not found.");

        var name = AddTradeHandler.Normalise(command.Name);

        // The list is curated: renaming onto a name another trade already holds would mint a
        // duplicate — refuse and point at consolidation instead (reassign the records, delete one).
        var clash = await context.Trades
            .FirstOrDefaultAsync(trade => trade.TradeId != command.TradeId && trade.Name.ToLower() == name.ToLower(), cancellationToken);
        if (clash is not null)
            throw new RenameTradeRefusedException($"“{clash.Name}” already exists — reassign this trade's companies to it and delete this one instead.");

        entity.Name = name;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
