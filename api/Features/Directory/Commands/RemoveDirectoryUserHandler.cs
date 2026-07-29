using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Directory;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

public sealed class RemoveDirectoryUserHandler
    : ICommandHandler<RemoveDirectoryUser, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly SignedInUserCache userCache;

    public RemoveDirectoryUserHandler(JpmsContext context, SignedInUserCache userCache)
    {
        this.context = context;
        this.userCache = userCache;
    }

    public async Task<Acknowledgement> HandleAsync(RemoveDirectoryUser command, CancellationToken cancellationToken)
    {
        var roleRows = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == command.Email)
            .ToListAsync(cancellationToken);
        context.DirectoryUserRoles.RemoveRange(roleRows);

        var entity = await context.DirectoryUsers
            .FirstOrDefaultAsync(user => user.Email == command.Email, cancellationToken);
        if (entity is not null) context.DirectoryUsers.Remove(entity);

        await context.SaveChangesAsync(cancellationToken);

        // The to-do fall-back rule: any to-do pinned to this person falls back to its assigned
        // role — whoever holds the role now sees it — rather than following someone who has left.
        // This is what keeps person-pinning safe: the pin never outlives the person.
        await context.TodoItems
            .Where(t => t.AssigneePersonEmail != null
                && t.AssigneePersonEmail.ToLower() == command.Email.ToLower())
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.AssigneePersonEmail, (string?)null),
                cancellationToken);

        // They no longer exist in the directory — drop any cached copy immediately rather than
        // letting a warm instance keep honouring their old permissions until the TTL lapses.
        userCache.InvalidateEmail(command.Email);
        return new Acknowledgement(command.Email);
    }
}
