using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Compose;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales.Inbox;

/// <summary>
/// The door a sales reply leaves by. It leaves from a second mailbox, so it cannot share the
/// projects mailbox's dispatcher instance — it runs the same sequence through one of its own.
/// That is the whole point of this class: until 2026-09-18 the sales reply staged and sent itself
/// and wrote nothing to the audit trail, which made it the one email the portal sent that nobody
/// could afterwards find. A lead is a linkable record like any other, so when the reply is being
/// written from a lead the row files under it.
/// </summary>
public sealed class SalesOutboundEmail
{
    private readonly OutboundEmailDispatcher? dispatcher;
    private readonly string address;

    private SalesOutboundEmail(OutboundEmailDispatcher? dispatcher, string address)
    {
        this.dispatcher = dispatcher;
        this.address = address;
    }

    public static SalesOutboundEmail Connected(IMailboxGraphClient mailbox, string address, AuditTrail audit) =>
        new(new OutboundEmailDispatcher(mailbox, audit, address), address);

    public static SalesOutboundEmail NotConnected(string address) => new(null, address);

    /// <summary>Reply-all to a message in the sales mailbox, with the body above the quoted
    /// history Graph supplies. LeadReference names the lead on the audit row.</summary>
    public async Task<SalesReplyOutcome> ReplyAsync(
        string messageId,
        string bodyHtml,
        string? leadId,
        string leadReference,
        CancellationToken cancellationToken)
    {
        if (dispatcher is null) return NotConnectedOutcome;
        var reply = new MailboxReplyDraftMessage(messageId, bodyHtml, Array.Empty<MailboxDraftAttachment>());
        var dispatch = await dispatcher.DispatchReplyAsync(
            reply, FilingFor(leadId, leadReference), saveAsDraftOnly: false, cancellationToken);
        return OutcomeFor(dispatch);
    }

    private SalesReplyOutcome NotConnectedOutcome =>
        new(false, null, $"The sales mailbox isn't connected on the API, so nothing was sent from {address}.");

    private OutboundEmailFiling FilingFor(string? leadId, string leadReference)
    {
        var pathway = AuditTrail.PathwayLabel(TriageCategories.Sales);
        var stagingRefused =
            $"The reply couldn't be staged in {address} — Graph refused. Check the Exchange access "
            + "policy includes that mailbox.";
        if (string.IsNullOrWhiteSpace(leadId))
            return new OutboundEmailFiling(pathway, stagingRefused, RecordReference: leadReference);
        return new OutboundEmailFiling(
            pathway, stagingRefused, null, RecordType.Lead, leadId, leadReference);
    }

    private SalesReplyOutcome OutcomeFor(OutboundEmailDispatch dispatch)
    {
        if (dispatch.Sent) return new SalesReplyOutcome(true, dispatch.WebLink, $"Sent from {address} to {Recipients(dispatch)}.");
        return new SalesReplyOutcome(
            false, dispatch.WebLink, dispatch.FailureNote ?? $"The reply is saved as a draft in {address}.");
    }

    private static string Recipients(OutboundEmailDispatch dispatch) =>
        string.Join(", ", (dispatch.To ?? Array.Empty<string>())
            .Concat(dispatch.Cc ?? Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase));
}
