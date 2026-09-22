using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.UsefulInformation;
using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// The shared site credential a Useful Information note may hold (2026-09-22, Jeremy's ask): held
// encrypted, never on the list model, set by anyone who manages notes, revealed by the directors
// alone. The reveal's audit row is the handler's job and reads the live AuditTrail; these tests
// pin the rules around it.
public sealed class UsefulInformationSecretTests
{
    private static readonly UsefulInformationOptions Configured =
        new() { SecretKey = Convert.ToBase64String(new byte[32]) };

    [Fact]
    public void Protector_roundTrips_andNeverStoresThePlaintext()
    {
        var protector = new SecretProtector(Configured);
        var stored = protector.Protect("Woodhouse-2026!");

        Assert.DoesNotContain("Woodhouse", stored);
        Assert.Equal("Woodhouse-2026!", protector.Unprotect(stored));
    }

    [Fact]
    public void Protector_isFreshPerValue_soTwoEqualSecretsStoreDifferently()
    {
        var protector = new SecretProtector(Configured);
        Assert.NotEqual(protector.Protect("4482#"), protector.Protect("4482#"));
    }

    [Fact]
    public void Protector_refusesWithoutAKey_onUseNotConstruction()
    {
        var protector = new SecretProtector(new UsefulInformationOptions());
        Assert.Throws<InvalidOperationException>(() => protector.Protect("4482#"));
    }

    [Fact]
    public async Task SettingASecret_holdsIt_andTheListModelSaysOnlyThatOneIsHeld()
    {
        await using var context = NewContext();
        context.UsefulInformationNotes.Add(Note("n1"));
        await context.SaveChangesAsync();

        var handler = new SetUsefulInformationSecretHandler(context, new SecretProtector(Configured));
        var note = await handler.HandleAsync(new SetUsefulInformationSecret("n1", "4482#", "site@jewelbb.co.uk"), CancellationToken.None);

        Assert.True(note.HasSecret);
        Assert.DoesNotContain("4482", System.Text.Json.JsonSerializer.Serialize(note));
        var stored = await context.UsefulInformationNotes.SingleAsync();
        Assert.NotNull(stored.SecretCiphertext);
        Assert.DoesNotContain("4482", stored.SecretCiphertext);
    }

    [Fact]
    public async Task ABlankSecret_removesTheCredential()
    {
        await using var context = NewContext();
        context.UsefulInformationNotes.Add(Note("n1", "ciphertext"));
        await context.SaveChangesAsync();

        var handler = new SetUsefulInformationSecretHandler(context, new SecretProtector(Configured));
        var note = await handler.HandleAsync(new SetUsefulInformationSecret("n1", "  ", "site@jewelbb.co.uk"), CancellationToken.None);

        Assert.False(note.HasSecret);
    }

    [Fact]
    public void Validation_refusesASecret_whenNoKeyIsConfigured()
    {
        var validation = new SetUsefulInformationSecretValidation(new UsefulInformationOptions());
        Assert.True(validation.Check(new SetUsefulInformationSecret("n1", "4482#")).HasFailed);
        Assert.False(validation.Check(new SetUsefulInformationSecret("n1", null)).HasFailed);
    }

    [Fact]
    public void Revealing_isTheDirectors_andHoldingIsEveryInternalRole()
    {
        Assert.True(UsefulInformationRoles.AllowedToReveal.IncludesAny(new[] { Role.ManagingDirector }));
        Assert.True(UsefulInformationRoles.AllowedToReveal.IncludesAny(new[] { Role.FinanceDirector }));
        Assert.False(UsefulInformationRoles.AllowedToReveal.IncludesAny(new[] { Role.SiteManager }));
        Assert.False(UsefulInformationRoles.AllowedToReveal.IncludesAny(new[] { Role.Architect }));

        var siteManager = new SignedInUser("site@jewelbb.co.uk", "Site", new[] { Role.SiteManager });
        Assert.True(new SetUsefulInformationSecretAuthorisation().Allows(siteManager, new SetUsefulInformationSecret("n1", "4482#")));
    }

    private static UsefulInformationNoteEntity Note(string id, string? ciphertext = null) => new()
    {
        UsefulInformationNoteId = id, ProjectId = "p1", Title = "Site WiFi", Body = "Router in the site office",
        CreatedByEmail = "site@jewelbb.co.uk", CreatedAt = DateTimeOffset.UtcNow, SecretCiphertext = ciphertext
    };

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"useful-information-secret-{Guid.NewGuid():N}").Options);
}
