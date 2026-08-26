using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

// Closes a request as at the user-chosen date. Until 2026-08-26 this was the agent framework's
// AttemptCloseRequest gate; with that framework retired the close is unconditional.
public sealed class CloseRequestHandler : ICommandHandler<CloseRequest, RequestCloseOutcome>
{
    private readonly JpmsContext context;
    public CloseRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<RequestCloseOutcome> HandleAsync(CloseRequest command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null) return new RequestCloseOutcome(Closed: false);

        request.Status = (int)RequestStatus.Closed;
        // The close date is user-chosen (validated as today or earlier) so a request closed after
        // the fact carries the date it actually closed, not the date someone got around to
        // recording it.
        request.ClosedAt = command.ClosedAt ?? DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return new RequestCloseOutcome(Closed: true);
    }
}
