using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed class ReviseQuoteHandler
    : ICommandHandler<ReviseQuote, Quote>
{
    private readonly JpmsContext context;

    public ReviseQuoteHandler(JpmsContext context) { this.context = context; }

    public async Task<Quote> HandleAsync(ReviseQuote command, CancellationToken cancellationToken)
    {
        var entity = await context.Quotes.FindAsync(new object[] { command.QuoteId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Quote {command.QuoteId} not found.");

        entity.Value = command.Value;
        entity.Notes = command.Notes;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
