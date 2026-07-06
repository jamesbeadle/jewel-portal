using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors;

// Shared lookups for the curated trade links. Directory reads always return each record's trades
// ordered by name so display and grouping are stable.
internal static class SubcontractorTradeQueries
{
    public static async Task<Dictionary<string, IReadOnlyList<Trade>>> TradesBySubcontractorAsync(
        this JpmsContext context, CancellationToken cancellationToken)
    {
        var links = await context.SubcontractorTrades
            .Join(context.Trades, link => link.TradeId, trade => trade.TradeId,
                (link, trade) => new { link.SubcontractorId, trade.TradeId, trade.Name })
            .ToListAsync(cancellationToken);

        return links
            .GroupBy(link => link.SubcontractorId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Trade>)group
                    .OrderBy(link => link.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(link => new Trade(link.TradeId, link.Name))
                    .ToList());
    }

    public static async Task<IReadOnlyList<Trade>> TradesForAsync(
        this JpmsContext context, string subcontractorId, CancellationToken cancellationToken)
    {
        var trades = await context.SubcontractorTrades
            .Where(link => link.SubcontractorId == subcontractorId)
            .Join(context.Trades, link => link.TradeId, trade => trade.TradeId, (link, trade) => trade)
            .ToListAsync(cancellationToken);

        return trades
            .OrderBy(trade => trade.Name, StringComparer.OrdinalIgnoreCase)
            .Select(trade => trade.ToModel())
            .ToList();
    }
}
