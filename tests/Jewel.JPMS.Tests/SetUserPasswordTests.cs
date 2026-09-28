using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Directory.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Directory;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>An administrator sets a user's password by hand on Admin → Users (2026-09-28): the
/// login can sign in with it at once, nothing opened under the old password survives, and the audit
/// row says who set whose — never the password.</summary>
public sealed class SetUserPasswordTests
{
    [Fact]
    public async Task SettingAPassword_letsThemSignInWithIt_andEndsEverythingOpenedBefore()
    {
        await using var context = await PasswordFixture.SeededAsync();
        await Handler(context).HandleAsync(Command(), CancellationToken.None);

        var credential = await context.UserCredentials.SingleAsync();
        var session = await context.UserSessions.SingleAsync();
        var connectedTool = await context.OAuthTokens.SingleAsync();
        var resetLink = await context.PasswordResetTokens.SingleAsync();
        Assert.True(PasswordHasher.Verify(PasswordFixture.NewPassword, credential.PasswordHash));
        Assert.False(PasswordHasher.Verify(PasswordFixture.OldPassword, credential.PasswordHash));
        Assert.Equal((int)CredentialStatus.Active, credential.Status);
        Assert.Equal(0, credential.FailedAttempts);
        Assert.NotNull(session.RevokedAt);
        Assert.NotNull(connectedTool.RevokedAt);
        Assert.NotNull(resetLink.ConsumedAt);
    }

    [Fact]
    public async Task AnInvitedUserWithNoCredential_isGivenOne_andCanSignIn()
    {
        await using var context = PasswordFixture.NewContext();
        context.DirectoryUsers.Add(new DirectoryUserEntity { Email = PasswordFixture.Email, DisplayName = "Jack Eastly" });
        await context.SaveChangesAsync();

        await Handler(context).HandleAsync(Command(), CancellationToken.None);

        var credential = await context.UserCredentials.SingleAsync();
        Assert.Equal((int)CredentialStatus.Active, credential.Status);
        Assert.True(PasswordHasher.Verify(PasswordFixture.NewPassword, credential.PasswordHash));
    }

    [Fact]
    public async Task TheAuditRow_namesWhoSetWhose_andNeverThePassword()
    {
        await using var context = await PasswordFixture.SeededAsync();
        await Handler(context).HandleAsync(Command(), CancellationToken.None);

        var row = await context.AuditEvents.SingleAsync();
        Assert.Equal((int)AuditEventType.PasswordSetByAdministrator, row.EventType);
        Assert.Equal(PasswordFixture.Administrator, row.ActorEmail);
        Assert.Contains(PasswordFixture.Email, row.Detail);
        Assert.DoesNotContain(PasswordFixture.NewPassword, row.Detail);
    }

    [Fact]
    public async Task ARevokedUser_andAnUnknownAddress_areRefused_andNothingChanges()
    {
        await using var revoked = await PasswordFixture.SeededAsync(revokedAt: DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(revoked).HandleAsync(Command(), CancellationToken.None));
        var untouched = await revoked.UserCredentials.SingleAsync();
        Assert.True(PasswordHasher.Verify(PasswordFixture.OldPassword, untouched.PasswordHash));

        await using var nobody = PasswordFixture.NewContext();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(nobody).HandleAsync(Command(), CancellationToken.None));
        Assert.Empty(await nobody.UserCredentials.ToListAsync());
    }

    [Fact]
    public void Validation_holdsThePasswordPolicy_andRefusesYourOwnPassword()
    {
        var validation = new SetUserPasswordValidation();
        Assert.False(validation.Check(Command()).HasFailed);
        Assert.True(validation.Check(Command() with { Password = "Short2026" }).HasFailed);
        Assert.True(validation.Check(Command() with { Password = "nouppercase2026" }).HasFailed);
        Assert.True(validation.Check(Command() with { SetByEmail = PasswordFixture.Email.ToUpperInvariant() }).HasFailed);
        Assert.True(validation.Check(Command() with { Email = " " }).HasFailed);
    }

    [Fact]
    public void OnlyAnAdministrator_maySetAPassword()
    {
        var authorisation = new SetUserPasswordAuthorisation();
        Assert.True(authorisation.Allows(Caller(Role.Admin), Command()));
        Assert.False(authorisation.Allows(Caller(Role.FinanceDirector), Command()));
        Assert.False(authorisation.Allows(Caller(Role.ManagingDirector), Command()));
    }

    private static SetUserPassword Command() =>
        new(PasswordFixture.Email, PasswordFixture.NewPassword, PasswordFixture.Administrator);

    private static SignedInUser Caller(Role role) => new(PasswordFixture.Administrator, "Caller", new[] { role });

    private static SetUserPasswordHandler Handler(JpmsContext context) =>
        new(context, PasswordFixture.Setter(context, new SignedInUserCache()), PasswordFixture.Audit(context));
}
