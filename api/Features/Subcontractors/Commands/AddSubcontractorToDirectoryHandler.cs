using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryHandler
    : ICommandHandler<AddSubcontractorToDirectory, Subcontractor>
{
    private readonly JpmsContext context;

    public AddSubcontractorToDirectoryHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(AddSubcontractorToDirectory command, CancellationToken cancellationToken)
    {
        // Trades come from the curated list — an unknown id is a caller bug, not data to store.
        var tradeIds = (command.TradeIds ?? Array.Empty<string>()).Distinct().ToList();
        var trades = await context.Trades.Where(trade => tradeIds.Contains(trade.TradeId)).ToListAsync(cancellationToken);
        if (trades.Count != tradeIds.Count)
            throw new InvalidOperationException("One or more trades were not found in the curated trade list.");

        var entity = new SubcontractorEntity
        {
            SubcontractorId = SubcontractorIdentifierFactory.NextSubcontractorId(),
            CompanyName = command.CompanyName,
            ContactName = command.ContactName,
            ContactEmail = command.ContactEmail,
            ContactPhone = command.ContactPhone,
            CisStatus = command.CisStatus,
            OnboardedAt = DateTimeOffset.UtcNow,
            Category = (int)command.Category,
            MobileNumber = command.MobileNumber,
            Town = command.Town,
            County = command.County,
            Website = command.Website
        };
        context.Subcontractors.Add(entity);
        foreach (var tradeId in tradeIds)
        {
            context.SubcontractorTrades.Add(new SubcontractorTradeEntity
            {
                SubcontractorTradeId = SubcontractorIdentifierFactory.NextSubcontractorTradeId(),
                SubcontractorId = entity.SubcontractorId,
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
