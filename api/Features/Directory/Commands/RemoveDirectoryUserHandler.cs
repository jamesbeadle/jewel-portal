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
        // They no longer exist in the directory — drop any cached copy immediately rather than
        // letting a warm instance keep honouring their old permissions until the TTL lapses.
        userCache.InvalidateEmail(command.Email);
        return new Acknowledgement(command.Email);
    }
}
