using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>
/// Revokes a user's access. This used to delete the directory row outright; it is now a soft
/// state (RevokedAt/RevokedBy on the row) so administrators can see who has been revoked and
/// restore them (RestoreDirectoryUser) — the role rows deliberately survive for exactly that.
/// What must NOT survive is the ability to sign in: the credential is disabled, every live
/// session is revoked, and any outstanding invite/reset link is voided, so revocation takes
/// effect now rather than when a cookie or emailed link next lapses.
/// </summary>
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
        var entity = await context.DirectoryUsers
            .FirstOrDefaultAsync(user => user.Email == command.Email, cancellationToken);
        if (entity is null || entity.RevokedAt is not null)
        {
            // Already gone or already revoked — revoking twice is a safe no-op.
            return new Acknowledgement(command.Email);
        }

        // Administrators are now administered in the directory like anyone else (no in-code
        // allow-list any more), so the directory itself has to refuse the one revocation that
        // cannot be undone from inside the app: taking away the last active Administrator.
        var targetIsAdmin = await context.DirectoryUserRoles
            .AnyAsync(row => row.DirectoryUserEmail == command.Email && row.Role == (int)Role.Admin,
                cancellationToken);
        if (targetIsAdmin)
        {
            var anotherActiveAdmin = await context.DirectoryUserRoles
                .Where(row => row.Role == (int)Role.Admin && row.DirectoryUserEmail != command.Email)
                .Join(context.DirectoryUsers.Where(user => user.RevokedAt == null),
                    row => row.DirectoryUserEmail, user => user.Email, (row, user) => user)
                .AnyAsync(cancellationToken);
            if (!anotherActiveAdmin)
                throw new InvalidOperationException(
                    "This is the last active administrator — appoint another Administrator before revoking this one.");
        }

        var now = DateTimeOffset.UtcNow;
        entity.RevokedAt = now;
        entity.RevokedBy = string.IsNullOrWhiteSpace(command.RevokedBy) ? null : command.RevokedBy.Trim();

        // They must not be able to sign in again until restored.
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == command.Email, cancellationToken);
        if (credential is not null) credential.Status = (int)CredentialStatus.Disabled;

        // Nor keep working on a session opened before the revocation…
        var liveSessions = await context.UserSessions
            .Where(row => row.Email == command.Email && row.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var session in liveSessions) session.RevokedAt = now;

        // …nor come back in through an invite or reset link that is still in someone's inbox.
        var liveTokens = await context.PasswordResetTokens
            .Where(row => row.Email == command.Email && row.ConsumedAt == null && row.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var token in liveTokens) token.ConsumedAt = now;

        await context.SaveChangesAsync(cancellationToken);

        // The to-do fall-back rule: any to-do pinned to this person falls back to its assigned
        // role — whoever holds the role now sees it — rather than following someone who has left.
        // This is what keeps person-pinning safe: the pin never outlives the person.
        await context.TodoItems
            .Where(t => t.AssigneePersonEmail != null
                && t.AssigneePersonEmail.ToLower() == command.Email.ToLower())
            .ExecuteUpdateAsync(set => set.SetProperty(t => t.AssigneePersonEmail, (string?)null),
                cancellationToken);

        // Drop any cached copy immediately rather than letting a warm instance keep honouring
        // their old permissions until the TTL lapses.
        userCache.InvalidateEmail(command.Email);
        return new Acknowledgement(command.Email);
    }
}
