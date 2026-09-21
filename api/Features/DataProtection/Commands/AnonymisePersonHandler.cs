using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.DataProtection;

namespace Jewel.JPMS.Api.Features.DataProtection.Commands;

/// <summary>
/// Erasure with the ledger intact: the rows that are the person lose their details, every
/// column that names them is rewritten to one pseudonym, and the sentences that quote them are
/// redacted in place. Saved in three steps so each pass reads what the one before left. Refused
/// while a sign-in still carries the address — that is a deletion, and DeleteDirectoryUser does
/// it — and while a worker under it still has history nobody has retired.
/// </summary>
public sealed class AnonymisePersonHandler : ICommandHandler<AnonymisePerson, PersonAnonymisation>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;

    public AnonymisePersonHandler(JpmsContext context, AuditTrail audit)
    {
        this.context = context;
        this.audit = audit;
    }

    public async Task<PersonAnonymisation> HandleAsync(AnonymisePerson command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim();
        await RefuseALiveLoginAsync(email, cancellationToken);
        await RefuseAnUnretiredWorkerAsync(email, cancellationToken);
        var pseudonym = PersonPseudonym.For(email);

        var names = await NamesHeldFor(email, cancellationToken);
        var recordsErased = await EraseRecordsAsync(email, pseudonym, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var mentions = await new PersonMentions(context).RewriteAsync(email, pseudonym, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var quotes = await new PersonFreeText(context).RewriteAsync(email, pseudonym, names, cancellationToken);
        await RemoveAccessRequestsAsync(email, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var rewritten = mentions.Concat(quotes).ToList();
        await audit.WriteAsync(AuditEventType.PersonAnonymised,
            $"{pseudonym}: {recordsErased} record(s) erased and {rewritten.Sum(mention => mention.Count)} mention(s) rewritten. Reason: {command.Reason.Trim()}",
            cancellationToken: cancellationToken);
        return new PersonAnonymisation(pseudonym, recordsErased, rewritten.Sum(mention => mention.Count), rewritten);
    }

    private async Task RefuseALiveLoginAsync(string email, CancellationToken cancellationToken)
    {
        var hasALogin = await context.DirectoryUsers.AnyAsync(user => user.Email == email, cancellationToken);
        if (hasALogin)
            throw new InvalidOperationException("This address still has a portal sign-in. Revoke the user and permanently delete them first — deletion removes the sign-in and pseudonymises their trail; anonymising then covers the rest.");
    }

    private async Task RefuseAnUnretiredWorkerAsync(string email, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.Where(worker => worker.ContactEmail == email && worker.RetiredAt == null).ToListAsync(cancellationToken);
        foreach (var worker in workers)
        {
            var hasHistory = await context.Timesheets.AnyAsync(timesheet => timesheet.WorkerId == worker.WorkerId, cancellationToken);
            if (hasHistory)
                throw new InvalidOperationException($"{worker.Name} is a worker with timesheet history. Retire them first (Labour → Workers → Retire), which clears their contact details and closes their engagement; their name stays because recorded cost is built on it.");
        }
    }

    private async Task<IReadOnlyCollection<string>> NamesHeldFor(string email, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        foreach (var kind in PersonRecordKinds.All)
        {
            var records = await kind.FindAsync(context, email, cancellationToken);
            names.AddRange(records.SelectMany(record => record.Fields)
                .Where(field => field.Label == PersonRecordFields.Name)
                .Select(field => field.Value));
        }
        return names;
    }

    private async Task<int> EraseRecordsAsync(string email, string pseudonym, CancellationToken cancellationToken)
    {
        var erased = 0;
        foreach (var kind in PersonRecordKinds.All)
            erased += await kind.EraseAsync(context, email, pseudonym, cancellationToken);
        return erased;
    }

    private async Task RemoveAccessRequestsAsync(string email, CancellationToken cancellationToken)
    {
        var requests = await context.AccessRequests.Where(request => request.Email == email).ToListAsync(cancellationToken);
        context.AccessRequests.RemoveRange(requests);
    }
}
