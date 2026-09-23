using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Forms.Retention;

/// <summary>What one night's chase asked for.</summary>
public sealed record FormRenewalChaseOutcome(int Insurance, int Training);

/// <summary>
/// The renewal chase: a sub-contractor's insurance and a person's ticket are asked for BEFORE the date,
/// not after — thirty days out, again a week later, three times at most — each with the form that
/// answers it attached as a one-time link, so replying is one tap. The insurance form tells the person
/// their expiry date will be used to remind them; this keeps that promise. A certificate with no
/// expiry is never chased, and nor is an ended one.
/// </summary>
public sealed partial class FormRenewalChase
{
    public const int DaysBeforeExpiry = 30;
    public const int DaysBetweenChases = 7;
    public const int MostChases = 3;
    private const int LinkDays = 14;
    private const string TheOffice = "The office";
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;

    public FormRenewalChase(JpmsContext context, IFormMailer mailer, FormSiteOptions options)
    {
        this.context = context;
        this.mailer = mailer;
        this.options = options;
    }

    public async Task<FormRenewalChaseOutcome> RunAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!mailer.IsConfigured) return new FormRenewalChaseOutcome(0, 0);
        var insurance = await ChaseInsuranceAsync(now, cancellationToken);
        var training = await ChaseTrainingAsync(now, cancellationToken);
        return new FormRenewalChaseOutcome(insurance, training);
    }

    private async Task<string?> SendAsync(IssuedInvite issued, Func<string, FormEmail> emailFor, CancellationToken cancellationToken)
    {
        var invite = issued.Invite;
        var link = options.FormLink((JewelCompany)invite.Company, invite.FormSlug, issued.Token);
        try
        {
            await mailer.SendAsync(emailFor(link), cancellationToken);
            context.FormInvites.Add(invite);
            return link;
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return null;
        }
    }

    private static bool IsDue(DateTimeOffset? lastChasedAt, int chaseCount, DateTimeOffset now)
    {
        var hasRestedLongEnough = lastChasedAt is null || lastChasedAt <= now.AddDays(-DaysBetweenChases);
        return chaseCount < MostChases && hasRestedLongEnough;
    }

    private static IssuedInvite NewInvite(string formSlug, FormLinkRecipient recipient, string reason, DateTimeOffset now) =>
        FormInviteRows.New(formSlug, recipient, new FormLinkSender("", TheOffice), now, now.AddDays(LinkDays), reason, null);
}
