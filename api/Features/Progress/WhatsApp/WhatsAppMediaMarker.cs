using System.Text.RegularExpressions;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// How an export says a message carried a file: iPhone <c>&lt;attached: name.jpg&gt;</c>, Android
/// <c>name.jpg (file attached)</c>, and — when the export was made without media —
/// <c>&lt;Media omitted&gt;</c> / <c>image omitted</c>, which names nothing.
/// </summary>
internal static class WhatsAppMediaMarker
{
    private static readonly Regex Attached = new(
        @"<attached:\s*(?<name>[^>]+)>|(?<name>[^\s<>]+\.[A-Za-z0-9]{2,5})\s\(file attached\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Omitted = new(
        @"<Media omitted>|\b(image|video|audio|sticker|document|GIF)\s+omitted\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public sealed record Split(string Text, string? MediaFileName, bool WasOmitted);

    public static Split Read(string text)
    {
        var attached = Attached.Match(text);
        if (attached.Success)
        {
            var remainder = text.Remove(attached.Index, attached.Length).Trim();
            return new Split(remainder, attached.Groups["name"].Value.Trim(), false);
        }
        var omitted = Omitted.Match(text);
        if (omitted.Success)
            return new Split(text.Remove(omitted.Index, omitted.Length).Trim(), null, true);
        return new Split(text.Trim(), null, false);
    }
}
