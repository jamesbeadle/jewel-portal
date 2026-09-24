using Jewel.JPMS.Api.Features.Forms.Office.Links;
using Jewel.JPMS.Api.Features.Forms.Public;
using Jewel.JPMS.Api.Features.Registers;
using Jewel.JPMS.Api.Features.Registers.Policies;
using Jewel.JPMS.Contracts.Forms;
using Jewel.JPMS.Contracts.Registers;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.PublicFormFixture;

namespace Jewel.JPMS.Tests;

// The Policy sign-off form (2026-09-24, the FD's task from Jeremy): sent by link to anyone, for the
// current revision of a published policy; the person reads it and signs its declaration with no portal
// login, and the signature lands on the Policies page against that exact revision. A new revision
// needs fresh signatures, and the form joins a new starter's pack only when the office ticks it in.
public sealed class PolicySignOffFormTests
{
    private const string Slug = FormSlugs.PolicySignOff;
    private const string Declaration = "I confirm I have read the Health & Safety Policy and will work to it.";

    [Fact]
    public async Task ItsOpenAddress_isNotValid_andCannotBeSentWithoutALink()
    {
        await using var forms = new PublicFormFixture();

        var opened = await forms.Service.OpenAsync(Slug, null, null, CancellationToken.None);
        var refusal = await Assert.ThrowsAsync<PublicFormRefusal>(() =>
            forms.Service.SubmitAsync(Slug, Posted(FirstSession, SignedAnswers()), Address, CancellationToken.None));

        Assert.Equal(FormLinkProblem.NotValid, opened!.Problem);
        Assert.StartsWith(FormWording.DeadLink(FormLinkProblem.NotValid).Heading, refusal.Message);
    }

    [Fact]
    public async Task ALink_showsTheRevision_andSigningRecordsItAgainstThatRevision()
    {
        await using var forms = new PublicFormFixture();
        var policy = await PublishAsync(forms, Declaration);
        var token = await SendAsync(forms, policy.PolicyDocumentId);
        var asked = await forms.Context.PolicySignOffs.AsNoTracking().SingleAsync();

        var opened = await forms.Service.OpenAsync(Slug, token, null, CancellationToken.None);
        var drawing = await SignAsync(forms, FirstSession);
        var tampered = SignedAnswers();
        tampered[PolicySignOffForm.DeclarationKey] = "Something else";
        await forms.Service.SubmitAsync(Slug, Posted(FirstSession, tampered, token, drawingId: drawing), Address, CancellationToken.None);

        Assert.Null(asked.SignedAt);
        Assert.NotNull(asked.FormInviteId);
        Assert.Equal(new PublicPolicy("Health & Safety Policy", 1, Declaration, false), opened!.Policy);
        Assert.Equal(Declaration, opened.Invitation!.Prefills[PolicySignOffForm.DeclarationKey]);
        var signed = await forms.Context.PolicySignOffs.SingleAsync();
        Assert.Equal(policy.PolicyDocumentId, signed.PolicyDocumentId);
        Assert.NotNull(signed.SignedAt);
        Assert.Equal("Sam Smith", signed.SignedName);
        Assert.Equal("Smith Groundworks Ltd", signed.CompanyName);
        Assert.Equal("Groundworker", signed.Position);
        var submission = await forms.Context.FormSubmissions.SingleAsync();
        Assert.Equal(submission.FormSubmissionId, signed.FormSubmissionId);
        Assert.Contains(Declaration, submission.AnswersJson);
        Assert.DoesNotContain("Something else", submission.AnswersJson);
    }

    [Fact]
    public async Task ANewRevision_needsFreshSignatures_andTheOldLinkCanNoLongerBeSigned()
    {
        await using var forms = new PublicFormFixture();
        var first = await PublishAsync(forms, "");
        var token = await SendAsync(forms, first.PolicyDocumentId);
        var second = await PublishAsync(forms, "");

        var opened = await forms.Service.OpenAsync(Slug, token, null, CancellationToken.None);
        var drawing = await SignAsync(forms, FirstSession);
        await Assert.ThrowsAsync<PublicFormRefusal>(() =>
            forms.Service.SubmitAsync(Slug, Posted(FirstSession, SignedAnswers(), token, drawingId: drawing), Address, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(forms, first.PolicyDocumentId));

        Assert.Equal(2, second.Revision);
        Assert.Equal(PolicyDeclarations.Standard, second.Declaration);
        Assert.Equal(FormLinkProblem.PolicySuperseded, opened!.Problem);
        Assert.False(await forms.Context.PolicySignOffs.AnyAsync(row => row.SignedAt != null));
    }

    [Fact]
    public async Task SomeoneWhoHasSigned_isNotAskedAgain()
    {
        await using var forms = new PublicFormFixture();
        var policy = await PublishAsync(forms, "");
        var token = await SendAsync(forms, policy.PolicyDocumentId);
        var drawing = await SignAsync(forms, FirstSession);
        await forms.Service.SubmitAsync(Slug, Posted(FirstSession, SignedAnswers(), token, drawingId: drawing), Address, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(forms, policy.PolicyDocumentId));
    }

    [Fact]
    public async Task ANewStarterPack_holdsThePolicySignOff_onlyWhenTickedIn()
    {
        await using var forms = new PublicFormFixture();
        var policy = await PublishAsync(forms, "");
        var answers = new FormPackAnswers(true, false, false, false);
        var packs = new SendFormPackHandler(forms.Context, forms.Mailer, forms.Options);

        var without = await packs.HandleAsync(new SendFormPack("Sam Smith", PersonEmail, Engagement.SelfEmployed, answers), CancellationToken.None);
        var with = await packs.HandleAsync(
            new SendFormPack("Alex Smith", "alex@example.com", Engagement.SelfEmployed, answers, PolicyDocumentId: policy.PolicyDocumentId),
            CancellationToken.None);

        Assert.DoesNotContain(without.Pack.Forms, form => form.FormSlug == Slug);
        var signOff = Assert.Single(with.Pack.Forms, form => form.FormSlug == Slug);
        Assert.Equal(policy.PolicyDocumentId, signOff.PolicyDocumentId);
        Assert.Equal("alex@example.com", (await forms.Context.PolicySignOffs.SingleAsync()).RecipientEmail);
    }

    [Fact]
    public async Task AChase_sendsAFreshLink_andTheOldOneStopsWorking()
    {
        await using var forms = new PublicFormFixture();
        var policy = await PublishAsync(forms, "");
        var oldToken = await SendAsync(forms, policy.PolicyDocumentId);
        var row = await forms.Context.PolicySignOffs.AsNoTracking().SingleAsync();
        var send = new SendFormInviteHandler(forms.Context, forms.Mailer, forms.Options);
        var resend = new ResendFormInviteHandler(forms.Context, forms.Mailer, forms.Options);

        var chased = await new ChasePolicySignOffHandler(forms.Context, resend, send)
            .HandleAsync(new ChasePolicySignOff(row.PolicySignOffId, "office@jewelbb.co.uk", "Jeremy"), CancellationToken.None);
        var oldLink = await forms.Service.OpenAsync(Slug, oldToken, null, CancellationToken.None);

        Assert.True(chased.IsEmailed);
        Assert.Equal(policy.PolicyDocumentId, chased.Invite.PolicyDocumentId);
        Assert.Equal(FormLinkProblem.NotValid, oldLink!.Problem);
        Assert.Equal(chased.Invite.FormInviteId, (await forms.Context.PolicySignOffs.AsNoTracking().SingleAsync()).FormInviteId);
    }

    private static Task<PolicyDocument> PublishAsync(PublicFormFixture forms, string declaration) =>
        new PublishPolicyDocumentHandler(forms.Context).HandleAsync(
            new PublishPolicyDocument("Health & Safety Policy", "", Array.Empty<string>(), declaration), "fd@jewelbb.co.uk", CancellationToken.None);

    private static async Task<string> SendAsync(PublicFormFixture forms, string policyDocumentId)
    {
        var command = new SendFormInvite(Slug, "Sam Smith", PersonEmail, "Smith Groundworks Ltd", 7, "", "office@jewelbb.co.uk", "Jeremy", policyDocumentId);
        await new SendFormInviteHandler(forms.Context, forms.Mailer, forms.Options).HandleAsync(command, CancellationToken.None);
        return RecordingFormMailer.FormSecretIn(forms.Mailer.Sent[^1]);
    }

    private static async Task<string> SignAsync(PublicFormFixture forms, string sessionId)
    {
        var drawing = new PublicFormUpload(sessionId, PolicySignOffForm.SignatureKey, "signature-signature.png", Convert.ToBase64String(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
        var receipt = await forms.Service.UploadAsync(Slug, drawing, Address, CancellationToken.None);
        return receipt.FormUploadId;
    }

    private static Dictionary<string, string> SignedAnswers() => new()
    {
        [PolicySignOffForm.ReadKey] = FormWording.Yes,
        [PolicySignOffForm.NameKey] = "Sam Smith",
        [PolicySignOffForm.CompanyKey] = "Smith Groundworks Ltd",
        [PolicySignOffForm.PositionKey] = "Groundworker",
        [FormAnswerRules.SignatureNameKey(PolicySignOffForm.SignatureKey)] = "Sam Smith"
    };
}
