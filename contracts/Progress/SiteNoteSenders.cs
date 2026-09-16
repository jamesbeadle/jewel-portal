namespace Jewel.JPMS.Contracts.Progress;

/// <summary>
/// The people whose WhatsApp messages are this project's site notes, held on the project as one
/// name per line (the site manager, and whoever else reports on this site). A name is matched as
/// WhatsApp shows the sender, case-insensitively, whitespace trimmed.
/// </summary>
public static class SiteNoteSenders
{
    public static IReadOnlyList<string> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return text
            .Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool Includes(IReadOnlyList<string> senders, string sender) =>
        senders.Any(name => string.Equals(name, sender.Trim(), StringComparison.OrdinalIgnoreCase));
}
