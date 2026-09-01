using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>
/// Reinstates a revoked user exactly as they were: the revocation marks are cleared and the role
/// rows never left, so their old permissions simply apply again. The credential comes back too —
/// Active if they had set a password (it still works), Invited if they never had (they need a
/// fresh invite link). Old sessions stay revoked: being restored means "may sign in again", not
/// "is signed in again".
/// </summary>
public sealed class RestoreDirectoryUserHandler
    : ICommandHandler<RestoreDirectoryUser, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly SignedInUserCache userCache;

    public RestoreDirectoryUserHandler(JpmsContext context, SignedInUserCache userCache)
    {
        this.context = context;
        this.userCache = userCache;
    }

    public async Task<Acknowledgement> HandleAsync(RestoreDirectoryUser command, CancellationToken cancellationToken)
    {
        var entity = await context.DirectoryUsers
            .FirstOrDefaultAsync(user => user.Email == command.Email, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("That user is no longer in the directory — their record may have been permanently deleted.");
        if (entity.RevokedAt is null)
        {
            // Already active — restoring twice is a safe no-op.
            return new Acknowledgement(command.Email);
        }

        entity.RevokedAt = null;
        entity.RevokedBy = null;

        var credential = await context.UserCredentials
            .FirstOrDefaultAsync(row => row.Email == command.Email, cancellationToken);
        if (credential is not null && credential.Status == (int)CredentialStatus.Disabled)
        {
            credential.Status = string.IsNullOrEmpty(credential.PasswordHash)
                ? (int)CredentialStatus.Invited
                : (int)CredentialStatus.Active;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Their permissions just changed — drop any cached copy so the next request re-reads them.
        userCache.InvalidateEmail(command.Email);
        return new Acknowledgement(command.Email);
    }
}
