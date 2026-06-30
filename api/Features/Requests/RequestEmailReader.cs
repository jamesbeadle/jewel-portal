using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests;

/// <summary>
/// Reads a request's associated emails LIVE from the mailbox by its workflow tag (JPMS/&lt;ref&gt;).
/// Nothing is stored — the tag is the only link. So removing the tag removes the email from the
/// record's context, and an email tagged to several records feeds all of them. Replaces the old
/// snapshot-into-RequestMessages approach: a request no longer keeps a copy of its emails.
/// </summary>
public sealed class RequestEmailReader
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;

    public RequestEmailReader(JpmsContext context, IMailboxGraphClient graph)
    {
        this.context = context;
        this.graph = graph;
    }

    /// <summary>All emails currently tagged to the request, oldest-first. Empty if the request is gone
    /// or has no tagged mail (or when Graph isn't configured — the null client returns nothing).</summary>
    public async Task<IReadOnlyList<MailboxMessage>> ForRequestAsync(string requestId, CancellationToken ct)
    {
        var request = await context.Requests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct);
        if (request is null)
            return Array.Empty<MailboxMessage>();

        var tag = TriageCategories.ForRequest(request.TagReference);

        var emails = new List<MailboxMessage>();
        string? cursor = null;
        var guard = 0;
        do
        {
            var page = await graph.ListByTagAsync(tag, cursor, 50, ct);
            emails.AddRange(page.Items);
            cursor = page.NextCursor;
        }
        while (cursor is not null && ++guard < 20);

        return emails.OrderBy(e => e.ReceivedAt).ToList();
    }
}
