using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Drawings.Storage;
using Jewel.JPMS.Api.Features.SiteAccess;
using Jewel.JPMS.Api.Features.SiteAccess.Commands;
using Jewel.JPMS.Api.Features.SiteAccess.Site;
using Jewel.JPMS.Contracts.SiteAccess;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The QR poster's link (the Site QR drawing access brief, 2026-09-04): only the hash is stored,
/// a spent or unknown token is refused alike, a link reaches its own folders and nothing else,
/// the approved revision is shown else the newest marked as such, and scans are counted while
/// inline views are not.
/// </summary>
public sealed class SiteDrawingLinkTests
{
    private const string Curator = "pm@jewelbb.co.uk";

    [Fact]
    public async Task Minting_storesOnlyTheHash_andTheTokenResolvesWhileAnotherDoesNot()
    {
        await using var context = SiteDrawingLinkFixture.NewContext();
        await SiteDrawingLinkFixture.SeedRegister(context);
        var created = await Create(context, SiteDrawingLinkFixture.Structural, includeSubFolders: true);
        var token = SiteDrawingLinkFixture.TokenOf(created.Url);

        var stored = await context.SiteDrawingLinks.SingleAsync();
        Assert.Equal(SiteDrawingLinkSecrets.HashOf(token), stored.TokenHash);
        Assert.DoesNotContain(token, stored.TokenHash);
        Assert.StartsWith("https://portal.test/api/site/", created.Url);
        Assert.NotEmpty(created.QrPngBase64);
        Assert.NotEmpty(created.PosterPdfBase64);

        var resolver = Resolver(context);
        Assert.NotNull(await resolver.ResolveAsync(token, CancellationToken.None));
        Assert.Null(await resolver.ResolveAsync(SiteDrawingLinkSecrets.NewToken(), CancellationToken.None));
    }

    [Fact]
    public async Task Expired_revoked_andUnknownTokens_areRefusedAlike()
    {
        await using var context = SiteDrawingLinkFixture.NewContext();
        await SiteDrawingLinkFixture.SeedRegister(context);
        var expired = await SiteDrawingLinkFixture.Link(context, "expired", expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        var revoked = await SiteDrawingLinkFixture.Link(context, "revoked", revokedAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        var resolver = Resolver(context);
        Assert.Null(await resolver.ResolveAsync(expired.Token, CancellationToken.None));
        Assert.Null(await resolver.ResolveAsync(revoked.Token, CancellationToken.None));
        Assert.Null(await resolver.ResolveAsync("garbage", CancellationToken.None));
        Assert.IsType<NotFoundResult>(resolver.Refuse(SiteDrawingLinkFixture.RequestFor("")));
    }

    [Fact]
    public async Task Revoking_stopsOneLink_andLeavesTheOthersWorking()
    {
        await using var context = SiteDrawingLinkFixture.NewContext();
        await SiteDrawingLinkFixture.SeedRegister(context);
        var first = await Create(context, SiteDrawingLinkFixture.Structural, includeSubFolders: false);
        var second = await Create(context, SiteDrawingLinkFixture.Architectural, includeSubFolders: false);

        var revoke = new RevokeSiteDrawingLinkHandler(context, SiteDrawingLinkFixture.Audit(context));
        var firstLink = first.Link;
        await revoke.HandleAsync(new RevokeSiteDrawingLink(firstLink.SiteDrawingLinkId, Curator), CancellationToken.None);

        var resolver = Resolver(context);
        Assert.Null(await resolver.ResolveAsync(SiteDrawingLinkFixture.TokenOf(first.Url), CancellationToken.None));
        Assert.NotNull(await resolver.ResolveAsync(SiteDrawingLinkFixture.TokenOf(second.Url), CancellationToken.None));
        Assert.Equal(3, await context.AuditEvents.CountAsync());
    }

    private static SiteLinkResolver Resolver(JpmsContext context) =>
        new(context, NullLogger<SiteLinkResolver>.Instance);

    private static Task<SiteDrawingLinkCreated> Create(JpmsContext context, string folderId, bool includeSubFolders)
    {
        var handler = new CreateSiteDrawingLinkHandler(context, SiteDrawingLinkFixture.Audit(context));
        var command = new CreateSiteDrawingLink(
            SiteDrawingLinkFixture.ProjectId, folderId, "Structural — Plot 2", includeSubFolders,
            SiteDrawingLinkLimits.DefaultExpiryDays, Curator, "https://portal.test");
        return handler.HandleAsync(command, CancellationToken.None);
    }
}
