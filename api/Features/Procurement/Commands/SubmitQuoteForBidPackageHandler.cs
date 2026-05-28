using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class SubmitQuoteForBidPackageHandler
    : ICommandHandler<SubmitQuoteForBidPackage, Quote>
{
    private readonly JpmsContext context;

    public SubmitQuoteForBidPackageHandler(JpmsContext context) { this.context = context; }

    public async Task<Quote> HandleAsync(SubmitQuoteForBidPackage command, CancellationToken cancellationToken)
    {
        var entity = new QuoteEntity
        {
            QuoteId = ProcurementIdentifierFactory.NextQuoteId(),
            BidPackageId = command.BidPackageId,
            SubcontractorId = command.SubcontractorId,
            Value = command.Value,
            Notes = command.Notes,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsDeclined = false
        };
        context.Quotes.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
