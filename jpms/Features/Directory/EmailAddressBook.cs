using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Features.Directory;

/// <summary>
/// The composers' address book — the directory's email recipients, fetched once per session and
/// searched in memory as the user types (a few hundred rows; a round-trip per keystroke would be
/// slower than the typing). Every RecipientInput shares this one instance, so opening a second
/// composer costs nothing. A failed load is swallowed: the picker degrades to a plain text field
/// (free-typed addresses always work) and the next composer retries.
/// </summary>
public sealed class EmailAddressBook
{
    private readonly IQueryClient queries;
    private Task<IReadOnlyList<EmailRecipient>>? loading;

    public EmailAddressBook(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<EmailRecipient> Recipients { get; private set; } = Array.Empty<EmailRecipient>();
    public bool Loaded { get; private set; }

    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (Loaded) return Task.CompletedTask;
        loading ??= LoadAsync(cancellationToken);
        return loading;
    }

    /// <summary>Drop the cached book so the next composer refetches — for after a directory edit.</summary>
    public void Invalidate()
    {
        Loaded = false;
        loading = null;
    }

    private async Task<IReadOnlyList<EmailRecipient>> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            Recipients = await queries.AskAsync(new ListEmailRecipients(), cancellationToken);
            Loaded = true;
        }
        catch
        {
            loading = null;
        }
        return Recipients;
    }

    /// <summary>Case-insensitive "contains" across name, email, organisation and detail, ranked so
    /// name-starts-with beats email-contains; already-picked addresses are left out.</summary>
    public IReadOnlyList<EmailRecipient> Search(string term, IReadOnlyCollection<string> excludeEmails, int take = 8)
    {
        var needle = (term ?? "").Trim();
        if (needle.Length == 0) return Array.Empty<EmailRecipient>();
        return Recipients
            .Where(recipient => !excludeEmails.Contains(recipient.Email, StringComparer.OrdinalIgnoreCase))
            .Select(recipient => (recipient, rank: RankOf(recipient, needle)))
            .Where(scored => scored.rank < int.MaxValue)
            .OrderBy(scored => scored.rank)
            .ThenBy(scored => scored.recipient.Name, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(scored => scored.recipient)
            .ToList();
    }

    private static int RankOf(EmailRecipient recipient, string needle)
    {
        const StringComparison ci = StringComparison.OrdinalIgnoreCase;
        if (recipient.Name.StartsWith(needle, ci)) return 0;
        if (recipient.Email.StartsWith(needle, ci)) return 1;
        // A surname: "smith" finds "John Smith".
        if (recipient.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(word => word.StartsWith(needle, ci))) return 2;
        if (recipient.Name.Contains(needle, ci) || recipient.Email.Contains(needle, ci)) return 3;
        if (recipient.Organisation?.Contains(needle, ci) == true) return 4;
        if (recipient.Detail?.Contains(needle, ci) == true) return 5;
        return int.MaxValue;
    }
}
