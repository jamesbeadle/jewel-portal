using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

/// <summary>
/// The public forms behind /f/&lt;company&gt;/&lt;form&gt; — no sign-in, as on the dashboard, because
/// labourers have no accounts. A form opens by its open address, by a one-time link for one named
/// person (?k=), or through a new starter's pack (?p=). With a link the page knows who it is for, the
/// office sees it was opened, and what is sent carries an identity rather than a typed name.
/// </summary>
public sealed partial class PublicFormService
{
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore store;
    private readonly IFormMailer mailer;
    private readonly FormSiteOptions options;
    private readonly ILogger<PublicFormService> logger;

    public PublicFormService(
        JpmsContext context, IFormEvidenceStore store, IFormMailer mailer, FormSiteOptions options, ILogger<PublicFormService> logger)
    {
        this.context = context;
        this.store = store;
        this.mailer = mailer;
        this.options = options;
        this.logger = logger;
    }

    public async Task<PublicFormView?> OpenAsync(
        string companyCode, string slug, string? inviteToken, string? packToken, CancellationToken cancellationToken)
    {
        var company = JewelCompanies.ForCode(companyCode);
        var form = FormCatalogue.For(slug);
        if (company is null || form is null) return null;
        var hasALink = !string.IsNullOrEmpty(inviteToken) || !string.IsNullOrEmpty(packToken);
        if (!hasALink) return new PublicFormView(form.Slug, company.Company, null, null);
        var link = await ResolveAsync(company.Company, form.Slug, inviteToken, packToken, cancellationToken);
        if (!link.IsOpen) return new PublicFormView(form.Slug, company.Company, null, link.Problem);
        await MarkOpenedAsync(link);
        var invitation = await InvitationForAsync(form, company.Company, link, cancellationToken);
        return new PublicFormView(form.Slug, company.Company, invitation, null);
    }

    private Task<ResolvedLink> ResolveAsync(
        JewelCompany company, string formSlug, string? inviteToken, string? packToken, CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(packToken)
            ? FormLinkResolution.ForInviteAsync(context, inviteToken, formSlug, company, cancellationToken)
            : FormLinkResolution.ForPackFormAsync(context, packToken, formSlug, company, cancellationToken);

    /// <summary>Best effort: a timestamp that will not write never stops a form being served.</summary>
    private async Task MarkOpenedAsync(ResolvedLink link)
    {
        var now = DateTimeOffset.UtcNow;
        link.Invite!.OpenedAt ??= now;
        if (link.Pack is { } pack) await KeepAliveAsync(pack, now);
        await SaveQuietlyAsync();
    }

    private async Task KeepAliveAsync(FormPackEntity pack, DateTimeOffset now)
    {
        var invites = await PackInvitesAsync(pack.FormPackId);
        FormPackLife.KeepAlive(pack, invites, now);
    }

    private Task<List<FormInviteEntity>> PackInvitesAsync(string formPackId) =>
        context.FormInvites.Where(row => row.FormPackId == formPackId && row.CancelledAt == null).ToListAsync();
}
