using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>
/// Permanently deletes a user's record: the directory row, its role rows, the credential, any
/// password tokens and every session. Only a row that is already revoked can be deleted — the
/// destructive step is always a second, deliberate act on an account that already cannot sign
/// in, never a single click on a live one.
/// </summary>
public sealed class DeleteDirectoryUserHandler
    : ICommandHandler<DeleteDirectoryUser, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly SignedInUserCache userCache;

    public DeleteDirectoryUserHandler(JpmsContext context, SignedInUserCache userCache)
    {
        this.context = context;
        this.userCache = userCache;
    }

    public async Task<Acknowledgement> HandleAsync(DeleteDirectoryUser command, CancellationToken cancellationToken)
    {
        var entity = await context.DirectoryUsers
            .FirstOrDefaultAsync(user => user.Email == command.Email, cancellationToken);
        if (entity is not null && entity.RevokedAt is null)
            throw new InvalidOperationException("Revoke this user first — permanent deletion is only available for revoked users.");

        var roleRows = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == command.Email)
            .ToListAsync(cancellationToken);
        context.DirectoryUserRoles.RemoveRange(roleRows);

        if (entity is not null) context.DirectoryUsers.Remove(entity);

        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == command.Email, cancellationToken);
        if (credential is not null) context.UserCredentials.Remove(credential);

        var tokens = await context.PasswordResetTokens
            .Where(row => row.Email == command.Email)
            .ToListAsync(cancellationToken);
        context.PasswordResetTokens.RemoveRange(tokens);

        var sessions = await context.UserSessions
            .Where(row => row.Email == command.Email)
            .ToListAsync(cancellationToken);
        context.UserSessions.RemoveRange(sessions);

        await context.SaveChangesAsync(cancellationToken);

        // Revocation already unpinned their to-dos, but belt-and-braces in case a pin was created
        // since (the fall-back rule: the pin never outlives the person).
        await context.TodoItems
            .Where(t => t.AssigneePersonEmail != null
                && t.AssigneePersonEmail.ToLower() == command.Email.ToLower())
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.AssigneePersonEmail, (string?)null),
                cancellationToken);

        userCache.InvalidateEmail(command.Email);
        return new Acknowledgement(command.Email);
    }
}
