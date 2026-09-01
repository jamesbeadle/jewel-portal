using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.MailboxIntake.Sharing;
using Jewel.JPMS.Api.Features.Progress.Storage;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Features.Requests.Documents;
using Jewel.JPMS.Api.Features.TenderEnquiries.Documents;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Api.Features.Variations.Documents;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    private static List<ComposeRecipient> CleanRecipients(IReadOnlyList<ComposeRecipient>? recipients) =>
        (recipients ?? Array.Empty<ComposeRecipient>())
        .Where(r => !string.IsNullOrWhiteSpace(r.Email) && r.Email.Contains('@'))
        .Select(r => new ComposeRecipient(r.Email.Trim(), string.IsNullOrWhiteSpace(r.Name) ? null : r.Name!.Trim()))
        .GroupBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
        .Select(g => g.First())
        .ToList();

    private static List<MailboxDraftRecipient> ToDraft(IReadOnlyList<ComposeRecipient> recipients) =>
        recipients.Select(r => new MailboxDraftRecipient(r.Email, r.Name)).ToList();

    private static string? MapPathway(string? pathway) =>
        string.Equals(pathway?.Trim(), "Client", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Client
        : string.Equals(pathway?.Trim(), "Subcontractor", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Subcontractor
        : string.Equals(pathway?.Trim(), "Supplier", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Supplier
        : string.Equals(pathway?.Trim(), "Internal", StringComparison.OrdinalIgnoreCase) ? TriageCategories.Internal
        : null;

    private static string Recipients(IReadOnlyList<string> to, IReadOnlyList<string> cc) =>
        cc.Count == 0
            ? $"to {string.Join("; ", to)}"
            : $"to {string.Join("; ", to)} (cc {string.Join("; ", cc)})";

    // The raised request's description wants readable text; an HTML body is stripped to its text.
    private static string PlainTextOf(SendMailboxEmail command)
    {
        if (!command.BodyIsHtml) return command.Body.Trim();
        var text = System.Text.RegularExpressions.Regex.Replace(command.Body, "<br\\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "</(p|div|li)>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        return System.Net.WebUtility.HtmlDecode(text).Trim();
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
