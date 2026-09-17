using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// The bulk run delegates each request to the single <see cref="SendRequestEmailHandler"/> — same
/// recipient resolution, same rendered PDF, same cover note, same choice of sending or leaving a
/// draft — so an email from here is indistinguishable from one sent on the request detail page.
///
/// They go one at a time (the Graph mailbox connection prefers a steady stream over a burst), and
/// user-fixable failures are captured per request rather than aborting the run: one request
/// without a resolvable recipient shouldn't stop the other nine going out. References are read up
/// front so every outcome can name its request (RFI-002) even when the id turns out not to
/// exist.
/// </summary>
public sealed class SendRequestEmailsHandler : ICommandHandler<SendRequestEmails, RequestEmailBatch>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<SendRequestEmail, RequestEmailOutcome> single;

    public SendRequestEmailsHandler(
        JpmsContext context,
        ICommandHandler<SendRequestEmail, RequestEmailOutcome> single)
    {
        this.context = context;
        this.single = single;
    }

    public async Task<RequestEmailBatch> HandleAsync(SendRequestEmails command, CancellationToken cancellationToken)
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

        var outcomes = new List<RequestEmailResult>(ids.Count);
        foreach (var id in ids)
        {
            references.TryGetValue(id, out var reference);
            try
            {
                var outcome = await single.HandleAsync(
                    new SendRequestEmail(id, SaveAsDraftOnly: command.SaveAsDraftOnly), cancellationToken);
                outcomes.Add(new RequestEmailResult(id, reference, outcome, null));
            }
            catch (InvalidOperationException ex)
            {
                // Missing recipients / unknown id / unconfigured mailbox are user-fixable — record
                // the message verbatim against this request and carry on with the rest.
                outcomes.Add(new RequestEmailResult(id, reference, null, ex.Message));
            }
        }

        return new RequestEmailBatch(outcomes);
    }
}
