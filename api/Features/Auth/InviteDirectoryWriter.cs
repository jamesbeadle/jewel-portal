using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Directory;
using Jewel.JPMS.Api.Gates;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>Writes the directory side of an invite: the user's roles, an invited credential, and
/// the voiding of any invite link still outstanding for that email.</summary>
public sealed class InviteDirectoryWriter
{
    private readonly JpmsContext context;
    private readonly SignedInUserCache userCache;

    public InviteDirectoryWriter(JpmsContext context, SignedInUserCache userCache)
    {
        this.context = context;
        this.userCache = userCache;
    }

    public async Task PrepareAsync(
        string email, string displayName, IReadOnlyList<Role> roles, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await UpsertDirectoryUserAsync(email, displayName, roles, cancellationToken);
        await EnsureCredentialAsync(email, now, cancellationToken);
        await InvalidatePreviousInvitesAsync(email, now, cancellationToken);
        // Re-inviting an existing user rewrites their role list, so any cached copy of them is now
        // wrong. (The caller still has to SaveChanges — this only drops the cache entry.)
        userCache.InvalidateEmail(email);
    }

    private async Task UpsertDirectoryUserAsync(string email, string displayName, IReadOnlyList<Role> roles, CancellationToken cancellationToken)
    {
        var directoryUser = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (directoryUser is null)
        {
            directoryUser = new DirectoryUserEntity { Email = email };
            context.DirectoryUsers.Add(directoryUser);
        }
        directoryUser.DisplayName = displayName;
        // Inviting an email that was revoked is a deliberate decision to bring them back — clear
        // the revocation rather than minting a link for an account that still cannot sign in.
        directoryUser.RevokedAt = null;
        directoryUser.RevokedBy = null;

        var existingRoles = await context.DirectoryUserRoles
            .Where(row => row.DirectoryUserEmail == email)
            .ToListAsync(cancellationToken);
        context.DirectoryUserRoles.RemoveRange(existingRoles);
        context.DirectoryUserRoles.AddRange(roles.Select(role => new DirectoryUserRoleEntity
        {
            DirectoryUserRoleId = DirectoryIdentifierFactory.NextRoleId(),
            DirectoryUserEmail = email,
            Role = (int)role
        }));
    }

    private async Task EnsureCredentialAsync(string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (credential is not null)
        {
            // A revoked-then-reinvited credential comes back as Invited: the fresh link (whose
            // completion sets it Active) is now the only way in, exactly like a first invite.
            if (credential.Status == (int)CredentialStatus.Disabled)
                credential.Status = (int)CredentialStatus.Invited;
            return;
        }
        context.UserCredentials.Add(new UserCredentialEntity
        {
            Email = email,
            Status = (int)CredentialStatus.Invited,
            CreatedAt = now
        });
    }

    private async Task InvalidatePreviousInvitesAsync(string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var liveInvites = await context.PasswordResetTokens
            .Where(row => row.Email == email && row.ConsumedAt == null && row.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var invite in liveInvites) invite.ConsumedAt = now;
    }
}
