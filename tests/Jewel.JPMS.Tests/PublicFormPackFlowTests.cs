using Jewel.JPMS.Api.Features.Forms.Office.Links;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.PublicFormFixture;

namespace Jewel.JPMS.Tests;

// A new starter's pack (the goal's "one link, done on a phone"): the office sends one link, the
// portal decides the forms, each form opens for the person the pack is for with what they already
// typed carried across — nothing sensitive — and the pack is complete when its last form is in.
public sealed class PublicFormPackFlowTests
{
    [Fact]
    public async Task APack_opensEachFormForItsPerson_andIsCompleteWhenItsLastFormIsIn()
    {
        await using var forms = new PublicFormFixture();
        var token = await SendPackAsync(forms);

        var fresh = await forms.Service.OpenPackAsync("jps", token, CancellationToken.None);
        await forms.Service.SubmitAsync("jps", FormSlugs.EmergencyContact, Posted(FirstSession, EmergencyAnswers(), packToken: token), Address, CancellationToken.None);
        var halfway = await forms.Service.OpenPackAsync("jps", token, CancellationToken.None);
        var halfwayPack = await forms.Context.FormPacks.AsNoTracking().SingleAsync();
        var drawing = await forms.SignAsync("jps", SecondSession);
        var rightToWork = Posted(SecondSession, RightToWorkAnswers(JewelCompany.JewelPropertyServe), packToken: token, drawingId: drawing.FormUploadId);
        var receipt = await forms.Service.SubmitAsync("jps", FormSlugs.RightToWork, rightToWork, Address, CancellationToken.None);

        Assert.Equal("Sam Smith", fresh!.PersonName);
        Assert.Equal(new[] { FormSlugs.EmergencyContact, FormSlugs.RightToWork }, fresh.Forms.Select(form => form.FormSlug));
        Assert.Equal(new[] { true, false }, halfway!.Forms.Select(form => form.IsDone));
        Assert.Null(halfwayPack.CompletedAt);
        Assert.True(receipt.IsVerified);
        var pack = await forms.Context.FormPacks.SingleAsync();
        Assert.NotNull(pack.CompletedAt);
        Assert.All(await forms.Context.FormSubmissions.ToListAsync(), submission => Assert.Equal(pack.FormPackId, submission.FormPackId));
    }

    [Fact]
    public async Task ALaterFormOfThePack_opensWithTheirDetails_butNothingSensitive()
    {
        await using var forms = new PublicFormFixture();
        var token = await SendPackAsync(forms);
        await forms.Service.SubmitAsync("jps", FormSlugs.EmergencyContact, Posted(FirstSession, EmergencyAnswers(), packToken: token), Address, CancellationToken.None);

        var opened = await forms.Service.OpenAsync("jps", FormSlugs.RightToWork, null, token, CancellationToken.None);

        var prefills = opened!.Invitation!.Prefills;
        Assert.Equal("Sam Smith", prefills["full_name"]);
        Assert.Equal(PersonEmail, prefills["email"]);
        Assert.Equal("Jewel Property Serve (JPS)", prefills["company"]);
        Assert.DoesNotContain(prefills.Values, value => value.Contains(Medical, StringComparison.Ordinal));
    }

    [Fact]
    public async Task APackLink_isRefusedForTheOtherJewelCompany()
    {
        await using var forms = new PublicFormFixture();
        var token = await SendPackAsync(forms);

        var elsewhere = await forms.Service.OpenPackAsync("jbb", token, CancellationToken.None);

        Assert.Equal(FormLinkProblem.NotValid, elsewhere!.Problem);
        Assert.Empty(elsewhere.Forms);
    }

    private static async Task<string> SendPackAsync(PublicFormFixture forms)
    {
        var pack = new SendFormPack(JewelCompany.JewelPropertyServe, "Sam Smith", PersonEmail, Engagement.SelfEmployed,
            new FormPackAnswers(false, false, false, false), "office@jewelps.co.uk", "Jeremy");
        await new SendFormPackHandler(forms.Context, forms.Mailer, forms.Options).HandleAsync(pack, CancellationToken.None);
        return RecordingFormMailer.PackSecretIn(Assert.Single(forms.Mailer.Sent));
    }
}
