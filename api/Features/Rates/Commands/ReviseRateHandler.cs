using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class ReviseRateHandler
    : ICommandHandler<ReviseRate, Rate>
{
    private readonly JpmsContext context;

    public ReviseRateHandler(JpmsContext context) { this.context = context; }

    public async Task<Rate> HandleAsync(ReviseRate command, CancellationToken cancellationToken)
    {
        var entity = await context.Rates.FindAsync(new object[] { command.RateId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Rate {command.RateId} not found.");

        entity.Trade = command.Trade;
        entity.Description = command.Description;
        entity.Unit = command.Unit;
        entity.Value = command.Value;
        entity.SupplierName = command.SupplierName;
        entity.LastPricedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
