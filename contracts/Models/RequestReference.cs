namespace Jewel.JPMS.Models;

/// <summary>
/// Generates the next human reference for a request (e.g. "RFI-049"). The next number is taken from
/// the <em>highest number already used</em> in the existing register, not from a positional count of
/// rows. This matters because the live/manual RFI register is authoritative: numbers can be
/// non-contiguous (gaps where an RFI was never digitised), rows can be deleted, and history is
/// back-filled out of order. Counting rows and adding one re-issues a number that is already in use;
/// incrementing the maximum never does.
/// </summary>
public static class RequestReference
{
    /// <summary>
    /// Suggests the next reference for a new request of <paramref name="kind"/>, continuing from the
    /// highest number found in <paramref name="existingReferences"/> (e.g. "RFI-048" → "RFI-049").
    /// Returns "{PREFIX}-001" when no numbered reference of that kind exists yet.
    /// </summary>
    public static string SuggestNext(RequestType kind, IEnumerable<string?> existingReferences)
    {
        var prefix = kind.DisplayName();
        var next = HighestNumber(prefix, existingReferences) + 1;
        return $"{prefix}-{next:000}";
    }

    /// <summary>
    /// The largest trailing number among references carrying <paramref name="prefix"/> (case-insensitive).
    /// References that don't match the "{prefix}-&lt;digits&gt;" shape are ignored so free-text entries
    /// never corrupt the sequence. Returns 0 when nothing matches.
    /// </summary>
    public static int HighestNumber(string prefix, IEnumerable<string?> references)
    {
        var highest = 0;
        foreach (var reference in references)
        {
            if (TryParseNumber(prefix, reference, out var number) && number > highest)
                highest = number;
        }
        return highest;
    }

    private static bool TryParseNumber(string prefix, string? reference, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(reference)) return false;

        var trimmed = reference.Trim();
        var marker = prefix + "-";
        if (!trimmed.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) return false;

        // Take the leading run of digits after the prefix so suffixed references still parse
        // (e.g. "RFI-049" and "RFI-049A" both yield 49).
        var digits = string.Empty;
        foreach (var character in trimmed[marker.Length..])
        {
            if (!char.IsDigit(character)) break;
            digits += character;
        }

        return int.TryParse(digits, out number);
    }
}
