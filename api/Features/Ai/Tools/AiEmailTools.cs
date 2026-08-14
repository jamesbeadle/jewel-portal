using System.Net;
using System.Text.Json;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Gates;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Ai.Tools;

/// <summary>
/// The assistant's one email capability, and deliberately its whole shape: a DRAFT in the projects
/// mailbox, through the same <c>CreateDraftAsync</c> chokepoint six human flows already use. This
/// is ADR-006 verbatim — agent-authored email is an Outlook draft the human opens, reads and sends
/// themselves; no assistant tool is (or may be) wired to SendDraftAsync. Mechanically it is
/// <see cref="AiToolKind.Read"/> (it executes server-side), but its blast radius is a deletable
/// draft: nothing reaches a third party until a person presses Send in Outlook.
/// </summary>
internal static class AiEmailTools
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private static string Serialise(object value) => JsonSerializer.Serialize(value, Json);
    private static string Fail(string message) => Serialise(new { ok = false, error = message });

    public static IReadOnlyList<AiTool> Build()
    {
        return new List<AiTool>
        {
            new(
                "draft_outlook_email",
                "Creates an email DRAFT in the projects mailbox. It lands in Outlook's Drafts folder "
                + "for the user to review and SEND THEMSELVES — nothing is sent by this tool, ever. "
                + "Never say an email has been sent; say a draft is ready and give them the link. Use "
                + "it when the user asks you to draft or write an email to someone — a variation "
                + "notice, a chase, a cover note. Body is plain text (it becomes simple HTML). Give "
                + "recordReference (V72, RFI-049, BPI-0010) when the email belongs to a record: the "
                + "draft is tagged so the SENT copy files itself into that record's correspondence.",
                AiToolSchema.Object(
                    ("to", "string", "Recipient email addresses, separated by semicolons.", true),
                    ("cc", "string", "Cc addresses, separated by semicolons.", false),
                    ("subject", "string", "The subject line, in the house style.", true),
                    ("body", "string",
                        "The email body as plain text. Write it ready to send — the user should only "
                        + "have to read it, not rewrite it. Quote figures ONLY from tool results.", true),
                    ("recordReference", "string",
                        "The record this email belongs to, as the user reads it (V72, RFI-049). "
                        + "Omit for general correspondence.", false)),
                AiToolKind.Read,
                JpmsRoleSets.CommercialTeam,
                async (context, input, ct) =>
                {
                    var to = ParseRecipients(AiToolSchema.Text(input, "to"));
                    if (to.Count == 0) return Fail("At least one recipient email address is required in `to`.");

                    var subject = AiToolSchema.Text(input, "subject")?.Trim();
                    var body = AiToolSchema.Text(input, "body")?.Trim();
                    if (string.IsNullOrWhiteSpace(subject)) return Fail("A subject is required.");
                    if (string.IsNullOrWhiteSpace(body)) return Fail("A body is required.");

                    var cc = ParseRecipients(AiToolSchema.Text(input, "cc"));
                    var reference = AiToolSchema.Text(input, "recordReference")?.Trim();

                    // The record tag rides on the DRAFT so the sent copy self-files into the
                    // record's correspondence — the same self-filing every portal reply relies on.
                    // Unsent drafts never surface on any read, so an abandoned draft tags nothing.
                    var categories = string.IsNullOrWhiteSpace(reference)
                        ? null
                        : new[] { TriageCategories.Marker, TriageCategories.ForRecord(reference!) };

                    var html = "<p>" + WebUtility.HtmlEncode(body)
                        .Replace("\r\n", "\n")
                        .Replace("\n\n", "</p><p>")
                        .Replace("\n", "<br/>") + "</p>";

                    MailboxDraft? draft;
                    try
                    {
                        var graph = context.Services.GetRequiredService<IMailboxGraphClient>();
                        draft = await graph.CreateDraftAsync(
                            new MailboxDraftMessage(
                                to, subject!, html,
                                Array.Empty<MailboxDraftAttachment>(),
                                Categories: categories,
                                Cc: cc.Count > 0 ? cc : null),
                            ct);
                    }
                    catch (Exception ex)
                    {
                        return Fail($"The draft could not be created ({ex.Message}).");
                    }

                    if (draft is null)
                    {
                        // ADR-007: declared, not hidden — the null client means Graph is unkeyed.
                        return Serialise(new
                        {
                            ok = false,
                            kind = "not_configured",
                            error = "The projects mailbox is not connected on this environment, so no draft "
                                    + "could be created. Offer the email text in the chat for the user to copy instead.",
                            operatorAction = "Configure the Graph mailbox credentials (MailboxIntake settings)."
                        });
                    }

                    return Serialise(new
                    {
                        ok = true,
                        draft_created = true,
                        webLink = draft.WebLink,
                        note = "A DRAFT only — nothing has been sent. Tell the user it is in the projects "
                               + "mailbox's Drafts folder, give them the link to open it in Outlook, and never "
                               + "describe it as sent."
                    });
                }),
        };
    }

    private static IReadOnlyList<MailboxDraftRecipient> ParseRecipients(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<MailboxDraftRecipient>()
            : raw.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(address => address.Contains('@', StringComparison.Ordinal))
                .Select(address => new MailboxDraftRecipient(address))
                .ToList();
}
