using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Rates;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Rates.Commands;

public sealed class AddRateHandler
    : ICommandHandler<AddRate, Rate>
{
    private readonly JpmsContext context;

    public AddRateHandler(JpmsContext context) { this.context = context; }

    public async Task<Rate> HandleAsync(AddRate command, CancellationToken cancellationToken)
    {
        var entity = new RateEntity
        {
            RateId = RateIdentifierFactory.Next(),
            Trade = command.Trade,
            Description = command.Description,
            Unit = command.Unit,
            Value = command.Value,
            SupplierName = command.SupplierName,
            LastPricedAt = DateTimeOffset.UtcNow
        };
        context.Rates.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
