using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Public;

public sealed partial class PublicFormService
{
    /// <summary>
    /// A new starter's pack behind its one link: what is done and what is left. Opening it keeps it
    /// alive, so a pack half done on the bus is still half done that evening.
    /// </summary>
    public async Task<PublicPackView> OpenPackAsync(string token, CancellationToken cancellationToken)
    {
        var link = await FormLinkResolution.ForPackAsync(context, token, cancellationToken);
        var pack = link.Pack;
        if (pack is null) return DeadPack(FormLinkProblem.NotValid);
        if (link.Problem is { } problem) return DeadPack(problem);
        var invites = await PackInvitesAsync(pack.FormPackId);
        var isStillOpen = pack.CompletedAt is null;
        if (isStillOpen) FormPackLife.KeepAlive(pack, invites, DateTimeOffset.UtcNow);
        await SaveQuietlyAsync();
        var forms = invites
            .OrderBy(invite => PlanOrder(invite.FormSlug))
            .Select(invite => new PublicPackForm(invite.FormSlug, invite.UsedAt is not null))
            .ToList();
        return new PublicPackView(pack.PersonName, forms, null);
    }

    private static PublicPackView DeadPack(FormLinkProblem problem) => new("", Array.Empty<PublicPackForm>(), problem);

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
