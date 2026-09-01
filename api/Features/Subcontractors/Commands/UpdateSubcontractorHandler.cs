using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpdateSubcontractorHandler
    : ICommandHandler<UpdateSubcontractor, Subcontractor>
{
    private readonly JpmsContext context;

    public UpdateSubcontractorHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(UpdateSubcontractor command, CancellationToken cancellationToken)
    {
        var entity = await context.Subcontractors.FindAsync(new object[] { command.SubcontractorId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Subcontractor {command.SubcontractorId} not found.");

        var tradeIds = (command.TradeIds ?? Array.Empty<string>()).Distinct().ToList();

        var existingLinks = await context.SubcontractorTrades
            .Where(link => link.SubcontractorId == command.SubcontractorId)
            .ToListAsync(cancellationToken);

        // Companies we buy work from must keep at least one trade — it's how bid-package invites
        // find them — but that is a rule about *changing* trades, not a toll on every save. Records
        // that arrived without any (the work-order seeds insert category-0 companies with no trade
        // links; the AddCuratedTrades backfill only linked a non-empty PrimaryTrade) would otherwise
        // be permanently un-editable, because the Edit company details dialog carries no trades
        // field and so round-trips the empty list straight back into this guard. Refuse only to
        // empty a list that has something in it.
        var needsTrade = (DirectoryCategory)entity.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier;
        if (needsTrade && tradeIds.Count == 0 && existingLinks.Count > 0)
            throw new InvalidOperationException("At least one trade is required for subcontractors and suppliers.");

        var trades = await context.Trades.Where(trade => tradeIds.Contains(trade.TradeId)).ToListAsync(cancellationToken);
        if (trades.Count != tradeIds.Count)
            throw new InvalidOperationException("One or more trades were not found in the curated trade list.");

        entity.CompanyName = command.CompanyName;
        entity.ContactName = command.ContactName;
        entity.ContactEmail = command.ContactEmail;
        entity.ContactPhone = command.ContactPhone;
        entity.CisStatus = command.CisStatus;
        // Null means "leave unchanged": callers that only touch trades or contact details
        // (SetTradesAsync, the bid-invite quick edits) never reset a per-company override.
        if (command.PaymentTermsDays is { } paymentTermsDays) entity.PaymentTermsDays = paymentTermsDays;
        // Same rule for the postal address — null leaves a field alone, empty string clears it.
        if (command.AddressLine is not null) entity.AddressLine = command.AddressLine;
        if (command.Town is not null) entity.Town = command.Town;
        if (command.County is not null) entity.County = command.County;
        if (command.Postcode is not null) entity.Postcode = command.Postcode;

        // Sync the trade links to exactly the requested set (add missing, remove dropped).
        context.SubcontractorTrades.RemoveRange(existingLinks.Where(link => !tradeIds.Contains(link.TradeId)));
        var existingTradeIds = existingLinks.Select(link => link.TradeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var tradeId in tradeIds.Where(id => !existingTradeIds.Contains(id)))
        {
            context.SubcontractorTrades.Add(new SubcontractorTradeEntity
            {
                SubcontractorTradeId = SubcontractorIdentifierFactory.NextSubcontractorTradeId(),
                SubcontractorId = command.SubcontractorId,
                TradeId = tradeId
            });
        }
        await context.SaveChangesAsync(cancellationToken);

        var tradeModels = trades
            .OrderBy(trade => trade.Name, StringComparer.OrdinalIgnoreCase)
            .Select(trade => trade.ToModel())
            .ToList();
        return entity.ToModel(tradeModels);
    }
}
