using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

public sealed partial class PublicFormService
{
    /// <summary>
    /// A new starter's pack behind its one link: what is done and what is left. Opening it keeps it
    /// alive, so a pack half done on the bus is still half done that evening.
    /// </summary>
    public async Task<PublicPackView?> OpenPackAsync(string companyCode, string token, CancellationToken cancellationToken)
    {
        var company = JewelCompanies.ForCode(companyCode);
        if (company is null) return null;
        var link = await FormLinkResolution.ForPackAsync(context, token, cancellationToken);
        var pack = link.Pack;
        var isThisCompanysPack = pack is not null && pack.Company == (int)company.Company;
        if (!isThisCompanysPack) return DeadPack(company.Company, FormLinkProblem.NotValid);
        if (link.Problem is { } problem) return DeadPack(company.Company, problem);
        var invites = await PackInvitesAsync(pack!.FormPackId);
        var isStillOpen = pack.CompletedAt is null;
        if (isStillOpen) FormPackLife.KeepAlive(pack, invites, DateTimeOffset.UtcNow);
        await SaveQuietlyAsync();
        var forms = invites
            .OrderBy(invite => PlanOrder(invite.FormSlug))
            .Select(invite => new PublicPackForm(invite.FormSlug, invite.UsedAt is not null))
            .ToList();
        return new PublicPackView(company.Company, pack.PersonName, forms, null);
    }

    private static PublicPackView DeadPack(JewelCompany company, FormLinkProblem problem) =>
        new(company, "", Array.Empty<PublicPackForm>(), problem);

    private static int PlanOrder(string formSlug)
    {
        var slugs = FormCatalogue.All.Select(definition => definition.Slug).ToList();
        return slugs.IndexOf(formSlug);
    }

    private async Task SaveQuietlyAsync()
    {
        try { await context.SaveChangesAsync(CancellationToken.None); }
        catch (DbUpdateException failure) { logger.LogWarning(failure, "Forms: could not keep a pack alive."); }
    }
}
