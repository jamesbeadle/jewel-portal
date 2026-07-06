using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

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
        // Companies we buy work from must keep at least one trade — it's how bid-package invites find them.
        var needsTrade = (DirectoryCategory)entity.Category is DirectoryCategory.Subcontractor or DirectoryCategory.Supplier;
        if (needsTrade && tradeIds.Count == 0)
            throw new InvalidOperationException("At least one trade is required for subcontractors and suppliers.");

        var trades = await context.Trades.Where(trade => tradeIds.Contains(trade.TradeId)).ToListAsync(cancellationToken);
        if (trades.Count != tradeIds.Count)
            throw new InvalidOperationException("One or more trades were not found in the curated trade list.");

        entity.CompanyName = command.CompanyName;
        entity.ContactName = command.ContactName;
        entity.ContactEmail = command.ContactEmail;
        entity.ContactPhone = command.ContactPhone;
        entity.CisStatus = command.CisStatus;

        // Sync the trade links to exactly the requested set (add missing, remove dropped).
        var existingLinks = await context.SubcontractorTrades
            .Where(link => link.SubcontractorId == command.SubcontractorId)
            .ToListAsync(cancellationToken);
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
