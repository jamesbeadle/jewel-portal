using Jewel.JPMS.Api.Features.Forms.Office.Links;
using Jewel.JPMS.Api.Features.Forms.Public;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.PublicFormFixture;

namespace Jewel.JPMS.Tests;

// A form sent from a phone with no account (moved from the JPS Dashboard, 2026-09-23): by its open
// address it is filed as unverified and the office is alerted without the health answers; by a
// one-time link it carries who it was sent to, the person gets their own copy, and the link is spent
// by the sending — never by the opening. A retry after a dropped signal answers with the first receipt.
public sealed class PublicFormFlowTests
{
    private const string Emergency = FormSlugs.EmergencyContact;

    [Fact]
    public async Task AnOpenAddressForm_isFiledUnverified_andItsAlertLeavesTheHealthAnswersOut()
    {
        await using var forms = new PublicFormFixture();

        var receipt = await forms.Service.SubmitAsync("jbb", Emergency, Posted(FirstSession, EmergencyAnswers()), Address, CancellationToken.None);

        Assert.False(receipt.IsVerified);
        var submission = await forms.Context.FormSubmissions.SingleAsync();
        Assert.Equal("Sam Smith", submission.SubmitterName);
        Assert.Contains(Medical, submission.AnswersJson);
        var alert = Assert.Single(forms.Mailer.Sent);
        Assert.Equal(new[] { JewelCompanies.BespokeBuild.Email }, alert.To);
        Assert.Contains("Alex Smith", alert.Text);
        Assert.DoesNotContain(Medical, alert.Text);
        Assert.DoesNotContain(Medical, alert.Html);
    }

    [Fact]
    public async Task SentTwice_answersWithTheFirstReceipt_andFilesOnce()
    {
        await using var forms = new PublicFormFixture();

        var first = await forms.Service.SubmitAsync("jbb", Emergency, Posted(FirstSession, EmergencyAnswers()), Address, CancellationToken.None);
        var retry = await forms.Service.SubmitAsync("jbb", Emergency, Posted(FirstSession, EmergencyAnswers()), Address, CancellationToken.None);

        Assert.Equal(first.FormSubmissionId, retry.FormSubmissionId);
        Assert.Equal(1, await forms.Context.FormSubmissions.CountAsync());
        Assert.Single(forms.Mailer.Sent);
    }

    [Fact]
    public async Task AMissingAnswer_isRefused_andNothingIsFiledOrSent()
    {
        await using var forms = new PublicFormFixture();
        var answers = EmergencyAnswers();
        answers.Remove("ec_phone");

        var refusal = await Assert.ThrowsAsync<PublicFormRefusal>(() =>
            forms.Service.SubmitAsync("jbb", Emergency, Posted(FirstSession, answers), Address, CancellationToken.None));

        Assert.Equal(FormWording.PleaseFillIn("Emergency contact's phone number"), refusal.Message);
        Assert.False(await forms.Context.FormSubmissions.AnyAsync());
        Assert.Empty(forms.Mailer.Sent);
    }

    [Fact]
    public async Task AOneTimeLink_vouchesForTheForm_isSpentBySending_andRefusedASecondTime()
    {
        await using var forms = new PublicFormFixture();
        var invite = new SendFormInvite(Emergency, JewelCompany.JewelPropertyServe, "Sam Smith", PersonEmail, "", 7, "", "office@jewelps.co.uk", "Jeremy");
        var sent = await new SendFormInviteHandler(forms.Context, forms.Mailer, forms.Options).HandleAsync(invite, CancellationToken.None);
        var token = RecordingFormMailer.FormSecretIn(Assert.Single(forms.Mailer.Sent));

        var opened = await forms.Service.OpenAsync("jps", Emergency, token, null, CancellationToken.None);
        var receipt = await forms.Service.SubmitAsync("jps", Emergency, Posted(FirstSession, EmergencyAnswers(), token), Address, CancellationToken.None);

        Assert.True(sent.IsEmailed);
        Assert.Equal("Sam Smith", opened!.Invitation!.Prefills["name"]);
        Assert.True(receipt.IsVerified);
        var row = await forms.Context.FormInvites.SingleAsync();
        Assert.Equal(receipt.FormSubmissionId, row.FormSubmissionId);
        Assert.NotNull(row.OpenedAt);
        Assert.NotNull(row.UsedAt);
        var copy = forms.Mailer.To(PersonEmail).Single(email => email.Subject.StartsWith("Your copy", StringComparison.Ordinal));
        Assert.Contains("Alex Smith", copy.Text);
        Assert.DoesNotContain(Medical, copy.Text);
        var used = FormWording.DeadLink(FormLinkProblem.Used, JewelCompanies.PropertyServe.Phone);
        var again = await Assert.ThrowsAsync<PublicFormRefusal>(() =>
            forms.Service.SubmitAsync("jps", Emergency, Posted(SecondSession, EmergencyAnswers(), token), Address, CancellationToken.None));
        Assert.Equal($"{used.Heading}. {used.Body}", again.Message);
        var reopened = await forms.Service.OpenAsync("jps", Emergency, token, null, CancellationToken.None);
        Assert.Equal(FormLinkProblem.Used, reopened!.Problem);
    }

    [Fact]
    public async Task ASignature_arrivesBeforeItsForm_inTheRestrictedStore_andTheAlertCarriesNoAnswers()
    {
        await using var forms = new PublicFormFixture();

        var drawing = await forms.SignAsync("jbb", FirstSession);
        var posted = Posted(FirstSession, RightToWorkAnswers(JewelCompany.JewelBespokeBuild), drawingId: drawing.FormUploadId);
        var receipt = await forms.Service.SubmitAsync("jbb", FormSlugs.RightToWork, posted, Address, CancellationToken.None);

        var upload = await forms.Context.FormUploads.SingleAsync();
        Assert.Equal(receipt.FormSubmissionId, upload.FormSubmissionId);
        Assert.Equal((int)FormEvidenceStore.RightToWork, upload.Store);
        Assert.True(forms.Store.Holds(FormEvidenceStore.RightToWork, upload.BlobRef));
        var alert = Assert.Single(forms.Mailer.Sent);
        Assert.DoesNotContain(Mobile, alert.Text);
        Assert.DoesNotContain(upload.FileName, alert.Html);
    }

    [Fact]
    public async Task AFile_isOnlyTakenByAQuestionThatAsksForOne()
    {
        await using var forms = new PublicFormFixture();
        var upload = new PublicFormUpload(FirstSession, "full_name", "passport.jpg", Convert.ToBase64String(new byte[] { 1, 2, 3 }));

        var refusal = await Assert.ThrowsAsync<PublicFormRefusal>(() =>
            forms.Service.UploadAsync("jbb", FormSlugs.RightToWork, upload, Address, CancellationToken.None));

        Assert.Equal("That question does not take a file.", refusal.Message);
        Assert.False(await forms.Context.FormUploads.AnyAsync());
    }
}
