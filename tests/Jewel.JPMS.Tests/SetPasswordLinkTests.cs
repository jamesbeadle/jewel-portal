using System.Text.Json;
using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Auth;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>The set-password link and an administrator setting a password go through one
/// PasswordSetter (2026-09-28): completing a link sets the password, consumes the link with every
/// other live one, and ends every session opened before it.</summary>
public sealed class SetPasswordLinkTests
{
    private const string InviteSecret = "invite-secret-from-the-email";

    [Fact]
    public async Task CompletingALink_setsThePassword_andVoidsEveryLiveLink_andEndsOldSessions()
    {
        await using var context = await SeededWithAnInviteAsync();

        var answer = await Endpoint(context).Run(RequestWith(InviteSecret, PasswordFixture.NewPassword));

        Assert.IsType<OkObjectResult>(answer);
        var credential = await context.UserCredentials.SingleAsync();
        var openedBefore = await context.UserSessions.SingleAsync(session => session.SessionId == PasswordFixture.OpenSession);
        Assert.True(PasswordHasher.Verify(PasswordFixture.NewPassword, credential.PasswordHash));
        Assert.All(await context.PasswordResetTokens.ToListAsync(), link => Assert.NotNull(link.ConsumedAt));
        Assert.NotNull(openedBefore.RevokedAt);
    }

    [Fact]
    public async Task ALinkAlreadyUsed_isRefused()
    {
        await using var context = await SeededWithAnInviteAsync();
        await Endpoint(context).Run(RequestWith(InviteSecret, PasswordFixture.NewPassword));

        var second = await Endpoint(context).Run(RequestWith(InviteSecret, "AnotherPassword2026"));

        Assert.IsType<BadRequestObjectResult>(second);
        var credential = await context.UserCredentials.SingleAsync();
        Assert.True(PasswordHasher.Verify(PasswordFixture.NewPassword, credential.PasswordHash));
    }

    private static async Task<JpmsContext> SeededWithAnInviteAsync()
    {
        var context = await PasswordFixture.SeededAsync();
        context.PasswordResetTokens.Add(PasswordFixture.Link(AuthTokens.Hash(InviteSecret), TokenPurpose.Invite));
        await context.SaveChangesAsync();
        return context;
    }

    private static SetPasswordEndpoint Endpoint(JpmsContext context)
    {
        var cache = new SignedInUserCache();
        return new SetPasswordEndpoint(context, new SessionManager(context, cache), PasswordFixture.Setter(context, cache));
    }

    private static HttpRequest RequestWith(string token, string password)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(new SetPasswordRequest(token, password)));
        httpContext.Request.ContentType = "application/json";
        return httpContext.Request;
    }
}
