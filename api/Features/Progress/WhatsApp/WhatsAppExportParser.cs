namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// Reads an exported chat into messages. A line that opens with a stamp starts a message; a line
/// that does not continues the message before it (WhatsApp writes multi-line messages that way);
/// a stamped line with no sender is WhatsApp's own notice (encryption, joins, deletions) and is
/// dropped. Pure — the same text always reads the same.
/// </summary>
public static class WhatsAppExportParser
{
    public sealed record Reading(IReadOnlyList<WhatsAppMessage> Messages, int OmittedMediaCount);

    public static Reading Parse(string exportText)
    {
        var messages = new List<WhatsAppMessage>();
        var omitted = 0;
        WhatsAppExportLine.Parsed? open = null;
        var openText = new List<string>();

        foreach (var rawLine in exportText.Split('\n'))
        {
            var parsed = WhatsAppExportLine.Parse(rawLine);
            if (parsed is null)
            {
                if (open?.Sender is not null) openText.Add(WhatsAppExportLine.Clean(rawLine));
                continue;
            }
            Close(open, openText, messages, ref omitted);
            open = parsed;
            openText = new List<string> { parsed.Rest };
        }
        Close(open, openText, messages, ref omitted);
        return new Reading(messages, omitted);
    }

    private static void Close(WhatsAppExportLine.Parsed? open, List<string> openText, List<WhatsAppMessage> messages, ref int omitted)
    {
        if (open?.Sender is null) return;
        var media = WhatsAppMediaMarker.Read(string.Join("\n", openText));
        if (media.WasOmitted) omitted++;
        messages.Add(new WhatsAppMessage(open.SentAt, open.Sender, media.Text, media.MediaFileName));
    }
}
