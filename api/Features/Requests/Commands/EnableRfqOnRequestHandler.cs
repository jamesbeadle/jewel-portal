using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Sets HasRfq on an RFI so a VOQ can be created from it. Requires the request to be an RFI.
/// </summary>
public sealed class EnableRfqOnRequestHandler : ICommandHandler<EnableRfqOnRequest, Request>
{
    private readonly JpmsContext context;
    public EnableRfqOnRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(EnableRfqOnRequest command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        if (entity.Kind != (int)RequestType.Rfi)
            throw new InvalidOperationException("An RFQ can only be enabled on a request that is an RFI.");

        entity.HasRfq = true;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
