using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Attachments;

/// <summary>
/// Downloads the attachments ticked on the architect's email — the PQQ, the drawings — from the
/// mailbox. Run FIRST, before anything persists, so "that attachment isn't there any more" is a
/// clean refusal rather than a half-attached enquiry.
/// </summary>
public sealed class TenderEnquiryEmailAttachmentFetcher
{
    private readonly IIntakeMessageReader reader;

    public TenderEnquiryEmailAttachmentFetcher(IIntakeMessageReader reader) { this.reader = reader; }

    public async Task<IReadOnlyList<TenderEnquiryIncomingFile>> DownloadAsync(
        string messageId, IReadOnlyList<string>? attachmentIds, CancellationToken cancellationToken)
    {
        var wanted = (attachmentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal);
        var files = new List<TenderEnquiryIncomingFile>();
        foreach (var attachmentId in wanted)
        {
            var attachment = await reader.GetAttachmentAsync(messageId, attachmentId, cancellationToken)
                ?? throw new InvalidOperationException(
                    "Couldn't download one of the ticked attachments from the mailbox — it may have "
                    + "been removed, or it isn't a file. Untick it and apply again.");
            files.Add(new TenderEnquiryIncomingFile(attachment.Name, attachment.ContentType, attachment.Content));
        }
        return files;
    }
}
