using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Contracts.Directory;

namespace Jewel.JPMS.Api.Features.Directory.Commands;

/// <summary>
/// Sets a user's password by hand (Admin → Users, 2026-09-28) through the same PasswordSetter the
/// set-password link uses, so the login ends Active with nothing opened under the old password left
/// alive. Only a user in the directory and not revoked takes one — restoring a revoked user comes
/// first — and the audit row names whose password was set and by whom, never the password.
/// </summary>
public sealed class SetUserPasswordHandler : ICommandHandler<SetUserPassword, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly PasswordSetter passwords;
    private readonly AuditTrail audit;

    public SetUserPasswordHandler(JpmsContext context, PasswordSetter passwords, AuditTrail audit)
    {
        this.context = context;
        this.passwords = passwords;
        this.audit = audit;
    }

    public async Task<Acknowledgement> HandleAsync(SetUserPassword command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim();
        await RequireAnActiveUserAsync(email, cancellationToken);
        await passwords.SetAsync(email, command.Password, cancellationToken);
        await audit.WriteAsync(
            AuditEventType.PasswordSetByAdministrator,
            $"Password set by hand for {email}; every session, connected tool and emailed link they had was ended.",
            actorEmail: command.SetByEmail,
            cancellationToken: cancellationToken);
        return new Acknowledgement(email);
    }

    private async Task RequireAnActiveUserAsync(string email, CancellationToken cancellationToken)
    {
        var user = await context.DirectoryUsers
            .FirstOrDefaultAsync(row => row.Email == email, cancellationToken);
        if (user is null)
            throw new InvalidOperationException("There's no user with that address. Add them on Admin → Users first.");
        var isRevoked = user.RevokedAt is not null;
        if (isRevoked)
            throw new InvalidOperationException("That user's access is revoked. Restore them before setting a password.");
    }
}
