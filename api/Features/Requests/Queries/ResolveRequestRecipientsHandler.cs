using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Requests.Recipients;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Queries;

/// <summary>
/// Previews the exact To/CC/BCC set an issue or draft would use right now, via the same shared
/// resolver as the send paths. Pure read — the preview can never disagree with a real send.
/// </summary>
public sealed class ResolveRequestRecipientsHandler : IQueryHandler<ResolveRequestRecipients, RequestRecipientSet>
{
    private readonly JpmsContext context;
    public ResolveRequestRecipientsHandler(JpmsContext context) { this.context = context; }

    public async Task<RequestRecipientSet> HandleAsync(ResolveRequestRecipients query, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == query.RequestId, cancellationToken);
        if (request is null) throw new InvalidOperationException($"Request '{query.RequestId}' not found.");

        return await RequestRecipientResolver.ResolveAsync(context, request, cancellationToken);
    }
}
