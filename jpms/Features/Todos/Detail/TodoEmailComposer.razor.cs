using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Features.Todos.Detail;

// The composer's working state and the send itself. A reply prefills its envelope reply-all from
// the opened email's live detail (sender or Reply-To in To; original To + Cc in Cc, minus the
// projects mailbox — Cc'ing it would deliver a copy straight back into the triage queue); a new
// email starts blank and is filed to the item by record link. The visible envelope is
// authoritative: what these fields show is exactly what goes on the wire.
public partial class TodoEmailComposer
{
    [Parameter, EditorRequired] public TodoItem Todo { get; set; } = default!;
    /// <summary>The linked email being replied to — null composes a brand-new outbound email.</summary>
    [Parameter] public MailboxMessage? ReplyTo { get; set; }
    /// <summary>Project pool for the attachment picker's drawing / photo sources.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();
    [Parameter] public EventCallback<ComposeOutcome> OnSent { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private MailboxMessageDetail? replyToDetail;
    private string toField = "";
    private string ccField = "";
    private string bccField = "";
    private bool showBcc;
    private string subject = "";
    private string body = "";
    private IReadOnlyList<ComposeDraftAttachment> attachments = Array.Empty<ComposeDraftAttachment>();
    private bool sending;
    private string? error;

    private bool IsReply => ReplyTo is not null;

    private string? ProjectIdOrNull => string.IsNullOrWhiteSpace(Todo.ProjectId) ? null : Todo.ProjectId;

    private bool IsSendable =>
        ParseRecipients(toField).Count > 0 && !string.IsNullOrWhiteSpace(subject) && HtmlHasContent(body);

    protected override async Task OnInitializedAsync()
    {
        if (ReplyTo is not { } replyTo) return;
        try
        {
            replyToDetail = await Intake.GetMessageDetailAsync(replyTo.Id, replyTo.InternetMessageId);
            PrefillReplyEnvelope(replyTo, replyToDetail);
        }
        catch
        {
            // The live detail also feeds the reply-all Cc set; without it the reply still works,
            // addressed to the sender alone.
            toField = replyTo.FromEmail;
            subject = ReplySubjectFor(replyTo.Subject);
        }
    }

    private void PrefillReplyEnvelope(MailboxMessage replyTo, MailboxMessageDetail loaded)
    {
        var toAddress = loaded.ReplyTo ?? loaded.FromEmail ?? replyTo.FromEmail;
        toField = toAddress ?? "";
        var ccAddresses = (loaded.To ?? Array.Empty<string>())
            .Concat(loaded.Cc ?? Array.Empty<string>())
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Where(address => !address.Equals(toAddress, StringComparison.OrdinalIgnoreCase))
            .Where(address => loaded.MailboxAddress is null
                || !address.Equals(loaded.MailboxAddress, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        ccField = string.Join("; ", ccAddresses);
        subject = ReplySubjectFor(loaded.Subject ?? replyTo.Subject);
    }

    private static string ReplySubjectFor(string? subject) =>
        string.IsNullOrWhiteSpace(subject) ? "RE: (no subject)"
        : subject.TrimStart().StartsWith("RE:", StringComparison.OrdinalIgnoreCase) ? subject.Trim()
        : $"RE: {subject.Trim()}";

    private async Task SendAsync()
    {
        if (sending || !IsSendable) return;
        error = null;
        sending = true;
        try
        {
            // A reply skips MarkThreadHandled: the thread already carries this item's tag (that is
            // why it is in the list), so there is nothing left to triage — and the sent copy
            // inherits the thread's tags, which is what files it here. A new email has no inbound
            // thread, so the explicit record link is what stamps the tag on its sent copy.
            var command = new SendMailboxEmail(
                ReplyToMessageId: ReplyTo?.Id,
                ReplyToInternetMessageId: ReplyTo?.InternetMessageId,
                To: ParseRecipients(toField),
                Cc: ParseRecipients(ccField),
                Bcc: ParseRecipients(bccField),
                Subject: subject.Trim(),
                Body: body,
                BodyIsHtml: true,
                Attachments: attachments.Select(attachment => attachment.ToRef()).ToList(),
                MarkThreadHandled: false,
                LinkRecordType: IsReply ? null : RecordType.Todo,
                LinkRecordId: IsReply ? null : Todo.TodoItemId,
                ProjectId: ProjectIdOrNull);
            var outcome = await Intake.SendComposedEmailAsync(command, UploadPartsOf(attachments));
            await OnSent.InvokeAsync(outcome);
        }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "The send didn't complete. Please try again."; }
        finally { sending = false; }
    }

    // A contenteditable's "empty" is markup like <div><br></div>: strip tags before judging —
    // except an inline image, which is real content with no text at all.
    private static bool HtmlHasContent(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return false;
        if (html.Contains("<img", StringComparison.OrdinalIgnoreCase)) return true;
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
        return !string.IsNullOrWhiteSpace(System.Net.WebUtility.HtmlDecode(text));
    }

    // "a@x; B <b@y>, c@z" → addresses. Display names in angle brackets are tolerated and stripped.
    private static List<ComposeRecipient> ParseRecipients(string field) =>
        (field ?? "")
        .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part =>
        {
            var open = part.LastIndexOf('<');
            var close = part.LastIndexOf('>');
            return open >= 0 && close > open ? part[(open + 1)..close].Trim() : part;
        })
        .Where(address => address.Contains('@'))
        .Select(address => new ComposeRecipient(address))
        .ToList();

    private static IReadOnlyList<(string PartName, Microsoft.AspNetCore.Components.Forms.IBrowserFile File)> UploadPartsOf(
        IReadOnlyList<ComposeDraftAttachment> attachments) =>
        attachments
            .Where(attachment => attachment.File is not null)
            .Select(attachment => (attachment.Key, attachment.File!))
            .ToList();
}
