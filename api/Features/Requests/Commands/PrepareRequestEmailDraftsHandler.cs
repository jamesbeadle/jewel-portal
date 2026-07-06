using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Bulk drafting delegates each request to the single <see cref="PrepareRequestEmailDraftHandler"/>
/// — same recipient resolution, same rendered PDF, same cover note — so a bulk-created draft is
/// indistinguishable from one prepared on the request detail page.
///
/// Drafts are created sequentially (the Graph mailbox connection prefers a steady stream over a
/// burst), and user-fixable failures are captured per request rather than aborting the run: one
/// request without a resolvable recipient shouldn't stop the other nine drafts landing in the
/// mailbox. References are read up front so every outcome can name its request (RFI-002) even
/// when the id turns out not to exist.
/// </summary>
public sealed class PrepareRequestEmailDraftsHandler : ICommandHandler<PrepareRequestEmailDrafts, RequestEmailDraftBatch>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft> single;

    public PrepareRequestEmailDraftsHandler(
        JpmsContext context,
        ICommandHandler<PrepareRequestEmailDraft, RequestEmailDraft> single)
    {
        this.context = context;
        this.single = single;
    }

    public async Task<RequestEmailDraftBatch> HandleAsync(PrepareRequestEmailDrafts command, CancellationToken cancellationToken)
    {
        var ids = command.RequestIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct()
            .ToList();

        var references = await context.Requests
            .Where(r => ids.Contains(r.RequestId))
            .Select(r => new { r.RequestId, r.Reference })
            .ToDictionaryAsync(r => r.RequestId, r => r.Reference, cancellationToken);

        var outcomes = new List<RequestEmailDraftOutcome>(ids.Count);
        foreach (var id in ids)
        {
            references.TryGetValue(id, out var reference);
            try
            {
                var draft = await single.HandleAsync(new PrepareRequestEmailDraft(id), cancellationToken);
                outcomes.Add(new RequestEmailDraftOutcome(id, reference, draft, null));
            }
            catch (InvalidOperationException ex)
            {
                // Missing recipients / unknown id / unconfigured mailbox are user-fixable — record
                // the message verbatim against this request and carry on with the rest.
                outcomes.Add(new RequestEmailDraftOutcome(id, reference, null, ex.Message));
            }
        }

        return new RequestEmailDraftBatch(outcomes);
    }
}
