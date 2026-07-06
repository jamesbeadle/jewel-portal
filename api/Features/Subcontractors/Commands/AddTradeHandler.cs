using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddTradeHandler : ICommandHandler<AddTrade, Trade>
{
    private readonly JpmsContext context;

    public AddTradeHandler(JpmsContext context) { this.context = context; }

    public async Task<Trade> HandleAsync(AddTrade command, CancellationToken cancellationToken)
    {
        var name = Normalise(command.Name);

        // The list is curated: adding a name that already exists (any casing) returns the
        // existing trade rather than minting a near-duplicate.
        var existing = await context.Trades
            .FirstOrDefaultAsync(trade => trade.Name.ToLower() == name.ToLower(), cancellationToken);
        if (existing is not null) return existing.ToModel();

        var entity = new TradeEntity
        {
            TradeId = SubcontractorIdentifierFactory.NextTradeId(),
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Trades.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // Trim and capitalise the first letter, preserving the rest ("drylining" → "Drylining").
    internal static string Normalise(string name)
    {
        var trimmed = (name ?? "").Trim();
        if (trimmed.Length == 0) return trimmed;
        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }
}
