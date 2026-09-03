using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Directory;

/// <summary>The whole address book in one read — every person the directory holds an email for,
/// de-duplicated by address. Small enough (hundreds of rows) to fetch once per session and filter
/// as the user types, so the composers never round-trip per keystroke.</summary>
public sealed record ListEmailRecipients : IQuery<IReadOnlyList<EmailRecipient>>;
