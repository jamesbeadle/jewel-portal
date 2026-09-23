using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>
/// Emailing a link, honestly (api/invite.js): the row exists and the link works whether or not the
/// email went, so a failed send is "created but not emailed" with the link in hand for the office to
/// send themselves — never a pretence that it failed.
/// </summary>
internal static class FormLinkMailing
{
    private const string NotEmailed = "The link was created but the email did not send: ";

    public static async Task<SentFormLink> SendOneAsync(
        IFormMailer mailer, FormSiteOptions options, FormDefinition form, IssuedInvite issued, CancellationToken cancellationToken)
    {
        var invite = issued.Invite;
        var company = (JewelCompany)invite.Company;
        var link = options.FormLink(company, form.Slug, issued.Token);
        var reason = invite.Reason.Length > 0 ? invite.Reason : FormWording.DefaultReason(form.TitleFor(company));
        var email = FormLinkEmails.ForOneForm(
            company, invite.PersonName, invite.Email, form.TitleFor(company), reason, invite.SentByName, link, invite.ExpiresAt);
        var problem = await TrySendAsync(mailer, email, cancellationToken);
        return new SentFormLink(invite.ToModel(), problem is null, problem is null ? "" : link, problem ?? "");
    }

    public static async Task<SentFormPack> SendPackAsync(
        IFormMailer mailer, FormSiteOptions options, FormPackEntity pack, IReadOnlyList<FormInviteEntity> invites, string token,
        string sentBy, bool isReminder, CancellationToken cancellationToken)
    {
        var company = (JewelCompany)pack.Company;
        var link = options.PackLink(company, token);
        var titles = invites.Where(invite => invite.UsedAt is null && invite.CancelledAt is null)
            .Select(invite => FormCatalogue.For(invite.FormSlug)?.TitleFor(company) ?? invite.FormSlug).ToList();
        var email = FormLinkEmails.ForPack(company, pack.PersonName, pack.Email, titles, sentBy, link, pack.ExpiresAt, isReminder);
        var problem = await TrySendAsync(mailer, email, cancellationToken);
        return new SentFormPack(pack.ToModel(invites), problem is null, problem is null ? "" : link, problem ?? "");
    }

    private static async Task<string?> TrySendAsync(IFormMailer mailer, FormEmail email, CancellationToken cancellationToken)
    {
        try
        {
            await mailer.SendAsync(email, cancellationToken);
            return null;
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            return NotEmailed + failure.Message;
        }
    }
}
