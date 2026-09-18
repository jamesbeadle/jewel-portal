namespace Jewel.JPMS.Contracts.MailboxCompose;

/// <summary>
/// One reading of "who is this addressed to", shared by the page and the server.
///
/// Recipient lists used to cross the wire as semicolon-separated strings — the composer's field
/// contents, sent verbatim — so the contract carried a text box rather than a list of addresses,
/// and the splitting rule lived in the handler where the page could not see it. Now the command
/// carries addresses and this is the one place that turns a typed field into them, so a list the
/// page shows as chips and a list the mailbox is handed cannot disagree.
/// </summary>
public static class RecipientList
{
    private static readonly char[] Separators = { ';', ',' };

    /// <summary>Addresses out of a field a person typed. Anything without an "@" is dropped rather
    /// than refused: a trailing separator and a half-typed name are normal in a field someone is
    /// still editing, and the door's own validation is what reports an empty result.</summary>
    public static IReadOnlyList<string> Parse(string? typed) =>
        (typed ?? "")
            .Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(address => address.Contains('@', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Back into a field, for seeding a composer from addresses already held.</summary>
    public static string Text(IReadOnlyList<string>? addresses) =>
        string.Join("; ", addresses ?? Array.Empty<string>());
}
