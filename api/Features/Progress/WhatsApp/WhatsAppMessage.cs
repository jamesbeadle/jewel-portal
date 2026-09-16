namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>One message as the export shows it: when it was sent (the phone's local clock, no
/// offset), who sent it, the words, and the media file it carried if any. A photograph sent
/// without a caption is a message with empty text and a file name.</summary>
public sealed record WhatsAppMessage(
    DateTime SentAt,
    string Sender,
    string Text,
    string? MediaFileName)
{
    public DateOnly Day => DateOnly.FromDateTime(SentAt);
    public bool HasMedia => MediaFileName is not null;
}
