using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>Every pack in flight and behind, newest first, each with its forms — the office's one screen of packs.</summary>
public sealed class ListFormPacksHandler : IQueryHandler<ListFormPacks, IReadOnlyList<FormPack>>
{
    private const int MostShown = 300;
    private readonly JpmsContext context;

    public ListFormPacksHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<FormPack>> HandleAsync(ListFormPacks query, CancellationToken cancellationToken)
    {
        var packs = await context.FormPacks.AsNoTracking()
            .OrderByDescending(row => row.SentAt).Take(MostShown).ToListAsync(cancellationToken);
        var packIds = packs.Select(pack => pack.FormPackId).ToList();
        var invites = await context.FormInvites.AsNoTracking()
            .Where(row => row.FormPackId != null && packIds.Contains(row.FormPackId))
            .ToListAsync(cancellationToken);
        return packs.Select(pack => pack.ToModel(invites)).ToList();
    }
}

/// <summary>Forms sent on their own, newest first — the Sent out list (api/invite.js ?list=1).</summary>
public sealed class ListFormInvitesHandler : IQueryHandler<ListFormInvites, IReadOnlyList<FormInvite>>
{
    private const int MostShown = 300;
    private readonly JpmsContext context;

    public ListFormInvitesHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<FormInvite>> HandleAsync(ListFormInvites query, CancellationToken cancellationToken)
    {
        var invites = await context.FormInvites.AsNoTracking()
            .Where(row => row.FormPackId == null)
            .OrderByDescending(row => row.SentAt).Take(MostShown).ToListAsync(cancellationToken);
        return invites.Select(invite => invite.ToModel()).ToList();
    }
}

public sealed class FormPackQueryEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListFormPacks, IReadOnlyList<FormPack>> packs;
    private readonly IQueryHandler<ListFormInvites, IReadOnlyList<FormInvite>> invites;

    public FormPackQueryEndpoints(
        SignedInUserResolver users,
        IQueryHandler<ListFormPacks, IReadOnlyList<FormPack>> packs,
        IQueryHandler<ListFormInvites, IReadOnlyList<FormInvite>> invites)
    {
        this.users = users;
        this.packs = packs;
        this.invites = invites;
    }

    [Function(nameof(ListFormPacks))]
    public async Task<IActionResult> ListPacks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-packs")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(packs, new ListFormPacks(), cancellationToken);
    }

    [Function(nameof(ListFormInvites))]
    public async Task<IActionResult> ListInvites(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "form-invites")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.Office.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(invites, new ListFormInvites(), cancellationToken);
    }
}
