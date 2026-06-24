using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Directory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>Writes the directory side of an invite: the user's roles, an invited credential, and
/// the voiding of any invite link still outstanding for that email.</summary>
public sealed class InviteDirectoryWriter
{
    private readonly JpmsContext context;

    public InviteDirectoryWriter(JpmsContext context)
    {
        this.context = context;
    }

    public async Task PrepareAsync(
        string email, string displayName, IReadOnlyList<Role> roles, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await UpsertDirectoryUserAsync(email, displayName, roles, cancellationToken);
        await EnsureCredentialAsync(email, now, cancellationToken);
        await InvalidatePreviousInvitesAsync(email, now, cancellationToken);
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
        if (credential is not null) return;
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
