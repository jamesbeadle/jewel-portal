using Jewel.JPMS.Api.Features.Ai.Sources;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

internal static partial class AiSourceTools
{
    internal static AiSourceManifest? ParseManifest(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<AiSourceManifest>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Attachments on the emails tagged to a record: the tagged list is one mailbox call,
    /// then one detail fetch per email that HAS attachments, newest first, under a wall clock so a
    /// slow mailbox costs the oldest emails their names rather than the turn. Returns the note to
    /// show, if any.</summary>
    private static async Task<string?> ListEmailAttachmentsAsync(
        AiToolContext context, RecordType recordType, string recordId, List<object> into, CancellationToken ct)
    {
        IReadOnlyList<MailboxMessage> messages;
        try
        {
            var emailReader = context.Services.GetRequiredService<RecordEmailReader>();
            messages = await emailReader.ForRecordAsync(recordType, recordId, ct);
        }
        catch (Exception ex)
        {
            return $"The mailbox could not be read ({ex.Message}).";
        }

        var withFiles = messages.Where(message => message.HasAttachments).OrderByDescending(message => message.ReceivedAt).ToList();
        if (withFiles.Count == 0)
            return messages.Count == 0
                ? "No emails are tagged to this record (or the mailbox is not configured)."
                : $"{messages.Count} tagged email{(messages.Count == 1 ? "" : "s")}, none with attachments.";

        var detailReader = context.Services.GetRequiredService<IIntakeMessageReader>();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var deadline = TimeSpan.FromSeconds(8);
        var skipped = 0;
        foreach (var message in withFiles)
        {
            if (clock.Elapsed > deadline) { skipped++; continue; }
            IntakeMessageContent? content;
            try
            {
                content = await detailReader.GetAsync(message.Id, ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                skipped++;
                continue;
            }
            if (content is null) { skipped++; continue; }

            foreach (var attachment in content.Attachments)
            {
                if (string.IsNullOrEmpty(attachment.Id)) continue;
                into.Add(new
                {
                    source_id = MailSourceId(message.Id, attachment.Id),
                    file = attachment.Name,
                    size = attachment.Size,
                    content_type = attachment.ContentType,
                    readable = AiSourceReader.IsSupported(attachment.Name, attachment.ContentType),
                    email = new
                    {
                        from = string.IsNullOrWhiteSpace(message.FromName) ? message.FromEmail : message.FromName,
                        subject = $"«{message.Subject}»",
                        received = message.ReceivedAt
                    }
                });
            }
        }

        return skipped == 0
            ? null
            : $"{skipped} email{(skipped == 1 ? "" : "s")} with attachments could not be listed in time — call again to retry.";
    }
}
