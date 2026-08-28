using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.RecordLinks.Commands;

// The connector's "File them all here" (the file_unfiled_replies action): the server-side twin of
// UnfiledRepliesNotice.FileAllAsync. Reads the record's unfiled replies through the SAME query the
// page banner uses, then files each through the SAME LinkMessageToRecord handler the page button
// posts — MessageOnly scope, so untagged thread siblings keep queueing for their own decisions.
// One refused reply (a cross-pathway conflict, an unreadable message) is reported in its outcome
// and never stops the rest. No HTTP endpoint — the portal has the banner button.

public sealed class FileUnfiledRepliesAuthorisation
{
    // Filing is a triage act — the banner button's CanFile and RecordLinksEndpoints.Gate read the
    // same set.
    public bool Allows(SignedInUser user, FileUnfiledReplies command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class FileUnfiledRepliesValidation
{
    public ValidationOutcome Check(FileUnfiledReplies command) =>
        string.IsNullOrWhiteSpace(command.RecordId)
            ? new ValidationOutcome(new[] { "recordId is required — find_by_reference resolves a reference to it." })
            : ValidationOutcome.Passed;
}

public sealed class FileUnfiledRepliesHandler : ICommandHandler<FileUnfiledReplies, FileUnfiledRepliesResult>
{
    private readonly RecordProviderRegistry providers;
    private readonly IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>> unfiled;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> linker;

    public FileUnfiledRepliesHandler(
        RecordProviderRegistry providers,
        IQueryHandler<ListUnfiledReplies, IReadOnlyList<MailboxMessage>> unfiled,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> linker)
    {
        this.providers = providers;
        this.unfiled = unfiled;
        this.linker = linker;
    }

    public async Task<FileUnfiledRepliesResult> HandleAsync(FileUnfiledReplies command, CancellationToken cancellationToken)
    {
        // The unfiled read answers "empty" for an unknown record; the connector deserves the
        // distinction between "nothing to file" and "no such record".
        if (!providers.TryGet(command.Type, out var provider))
            throw new InvalidOperationException($"{command.Type} records don't carry tagged mail.");
        var record = await provider.FindAsync(command.RecordId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"{command.Type} record '{command.RecordId}' not found — find_by_reference resolves a reference to its id.");

        var replies = await unfiled.HandleAsync(new ListUnfiledReplies(command.Type, command.RecordId), cancellationToken);
        var outcomes = new List<FiledReplyOutcome>(replies.Count);
        foreach (var reply in replies)
        {
            try
            {
                // Exactly the banner button's command (UnfiledRepliesNotice.FileAllAsync).
                await linker.HandleAsync(new LinkMessageToRecord(
                        reply.Id, command.Type, command.RecordId, reply.InternetMessageId,
                        Scope: LinkThreadScope.MessageOnly),
                    cancellationToken);
                outcomes.Add(new FiledReplyOutcome(reply.Subject, reply.FromName, reply.FromEmail, reply.ReceivedAt, Filed: true));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                outcomes.Add(new FiledReplyOutcome(reply.Subject, reply.FromName, reply.FromEmail, reply.ReceivedAt, Filed: false, Error: ex.Message));
            }
        }
        return new FileUnfiledRepliesResult(outcomes.Count, outcomes.Count(outcome => outcome.Filed), outcomes);
    }
}
