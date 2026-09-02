using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Api.Features.MailboxIntake.Compose;

public sealed partial class SendMailboxEmailHandler
{
    /// <summary>One compose as it moves through the pipeline: what the command settled at
    /// validation, then what each step adds — the anchor read, the body and attachments, the
    /// record filing, the staged draft. The derived facts the audit rows and the outcome read are
    /// properties, so they always reflect the latest step.</summary>
    private sealed class Compose
    {
        public SendMailboxEmail Command { get; }
        public bool IsReply { get; }
        public bool IsForward { get; }
        public string Subject { get; }
        public List<ComposeRecipient> To { get; }
        public List<ComposeRecipient> Cc { get; }
        public List<ComposeRecipient> Bcc { get; }
        /// <summary>The triager's explicit pathway, as a bucket tag — or null for none.</summary>
        public string? ChosenBucket { get; }
        /// <summary>A handled reply tags the inbound thread after the send. A pathway-less reply is
        /// allowed: answering IS dealing with the email, so the thread is triaged with JPMS/Replied
        /// alone and no bucket — choosing a side in System Tags (or any record filing) is what files
        /// it under a pathway. A FORWARD never handles the thread — passing an email on isn't
        /// answering it, so it stays in the queue unless something else files it.</summary>
        public bool WillHandleThread { get; }

        // The replied-to email, read fresh (ids + thread context); null for a new email.
        public MailboxSnapshot? Snapshot { get; set; }
        // An existing thread bucket always wins (the composer shows it as fixed); otherwise the
        // triager's explicit choice files the thread when the reply triages it.
        public string? ExistingBucket { get; set; }
        public string? EffectiveBucket { get; set; }

        public string BodyHtml { get; set; } = "";
        /// <summary>Everything the draft carries: the resolved files plus the body's inline images.</summary>
        public List<MailboxDraftAttachment> Attachments { get; set; } = new();

        public Request? RaisedRequest { get; set; }
        public string? RecordTag { get; set; }
        public LinkableRecord? LinkedRecord { get; set; }

        /// <summary>The workflow tags stamped on the sent copy — what the to-do activity reads.</summary>
        public List<string> WorkflowStamp { get; set; } = new();
        /// <summary>Marker + stamp + pathway, or nothing at all — what the draft is created with.</summary>
        public List<string> DraftCategories { get; set; } = new();
        public string DraftId { get; set; } = "";
        public string? WebLink { get; set; }

        public List<string> ToAddresses => To.Select(r => r.Email).ToList();
        public List<string> CcAddresses => Cc.Select(r => r.Email).ToList();
        public List<MailboxDraftRecipient> DraftTo => ToDraft(To);
        public List<MailboxDraftRecipient> DraftCc => ToDraft(Cc);
        public List<MailboxDraftRecipient> DraftBcc => ToDraft(Bcc);
        public string SenderEmail => Command.SenderEmail;
        public string PathwayLabel => AuditTrail.PathwayLabel(EffectiveBucket);
        public string? ProjectId => NullIfEmpty(Command.ProjectId) ?? NullIfEmpty(LinkedRecord?.ProjectId);
        /// <summary>The thread was filed to a record on the way — a raised request, or a link on a reply.</summary>
        public bool FiledToRecord => RaisedRequest is not null || (IsReply && RecordTag is not null);
        public RecordType? AuditRecordType => LinkedRecord?.Type ?? (RaisedRequest is not null ? RecordType.Request : (RecordType?)null);
        public string? AuditRecordId => LinkedRecord?.RecordId ?? RaisedRequest?.RequestId;
        public string AuditRecordReference => LinkedRecord?.Reference ?? RaisedRequest?.Reference ?? "";

        public ComposeOutcome Outcome(bool sent, string? webLink, bool threadHandled, string? failureNote) => new(
            DraftId, webLink, sent, Subject, ToAddresses, CcAddresses,
            ThreadHandled: threadHandled,
            FailureNote: failureNote,
            RaisedRequest: RaisedRequest);

        private Compose(SendMailboxEmail command, bool isReply, bool isForward, string subject,
            List<ComposeRecipient> to, List<ComposeRecipient> cc, List<ComposeRecipient> bcc)
        {
            Command = command;
            IsReply = isReply;
            IsForward = isForward;
            Subject = subject;
            To = to;
            Cc = cc;
            Bcc = bcc;
            ChosenBucket = MapPathway(command.Pathway);
            WillHandleThread = isReply && !isForward && command.MarkThreadHandled;
        }

        /// <summary>The command checked before anything is read or created, each refusal in the
        /// words the composer shows.</summary>
        public static Compose Validated(SendMailboxEmail command)
        {
            var isReply = !string.IsNullOrWhiteSpace(command.ReplyToMessageId);
            var isForward = isReply && command.Forward;
            var subject = command.Subject?.Trim() ?? "";
            var to = CleanRecipients(command.To);
            var cc = CleanRecipients(command.Cc);
            var bcc = CleanRecipients(command.Bcc);

            if (to.Count + cc.Count + bcc.Count == 0)
                throw new InvalidOperationException("Add at least one recipient before sending.");
            if (to.Count == 0)
                throw new InvalidOperationException("Add a To recipient (Cc/Bcc-only emails are refused by most mail servers).");
            if (subject.Length == 0)
                throw new InvalidOperationException("Write a subject before sending.");
            if (string.IsNullOrWhiteSpace(command.Body))
                throw new InvalidOperationException("Write the email before sending.");
            if (command.AlsoRaiseRequest && !isReply)
                throw new InvalidOperationException("A request can only be raised from a reply to an email.");
            if (command.AlsoRaiseRequest && isForward)
                throw new InvalidOperationException("A request is raised from a reply, not a forward.");
            if (command.AlsoRaiseRequest && string.IsNullOrWhiteSpace(command.ProjectId))
                throw new InvalidOperationException("Choose the project the request is raised on.");
            if (command.AlsoRaiseRequest && command.LinkRecordType is not null)
                throw new InvalidOperationException("Raise a request or link an existing record — not both in one send.");

            return new Compose(command, isReply, isForward, subject, to, cc, bcc);
        }
    }

    private async Task ReadAnchorAsync(Compose compose, CancellationToken cancellationToken)
    {
        var command = compose.Command;
        if (compose.IsReply)
            compose.Snapshot = await graph.GetSnapshotAsync(
                    command.ReplyToMessageId!, command.ReplyToInternetMessageId, cancellationToken)
                ?? throw new InvalidOperationException("The email you're replying to could not be read from the mailbox.");

        compose.ExistingBucket = (compose.Snapshot?.Categories ?? Array.Empty<string>())
            .FirstOrDefault(TriageCategories.IsBucketTag);
        compose.EffectiveBucket = compose.ExistingBucket
            ?? (command.AlsoRaiseRequest ? TriageCategories.Client : compose.ChosenBucket);
    }
}
