using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// The in-app invite composer's three endpoints: read the persisted draft, save it, send the
/// invite. Sending an invite (and drafting one) is exactly the set who could create the draft in
/// the old Outlook flow — PrepareBidPackageInviteDraftAuthorisation's roles — reused so moving
/// the send in-app widened nobody's reach.
/// </summary>
public sealed class BidPackageInviteComposerEndpoints
{
    private static readonly RoleSet AllowedToInvite = RoleSet.Of(
        Role.Admin, JpmsRoles.Director, JpmsRoles.ProjectManager,
        JpmsRoles.Estimator, JpmsRoles.OfficeComplianceCoordinator, JpmsRoles.OfficeAdmin, JpmsRoles.SalesMarketing);

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?> get;
    private readonly ICommandHandler<SaveBidPackageInviteComposerDraft, Acknowledgement> save;
    private readonly ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome> send;

    public BidPackageInviteComposerEndpoints(
        SignedInUserResolver users,
        IQueryHandler<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?> get,
        ICommandHandler<SaveBidPackageInviteComposerDraft, Acknowledgement> save,
        ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome> send)
    {
        this.users = users; this.get = get; this.save = save; this.send = send;
    }

    [Function(nameof(GetBidPackageInviteComposerDraft))]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bid-packages/{bidPackageId}/invite-draft")] HttpRequest request,
        string bidPackageId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToInvite.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        return new OkObjectResult(await get.HandleAsync(new GetBidPackageInviteComposerDraft(bidPackageId), cancellationToken));
    }

    [Function(nameof(SaveBidPackageInviteComposerDraft))]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/invite-draft")] HttpRequest request,
        string bidPackageId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToInvite.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var command = await request.ReadFromJsonAsync<SaveBidPackageInviteComposerDraft>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");

        return new OkObjectResult(await save.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(SendBidPackageInvite))]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bid-packages/{bidPackageId}/send-invite")] HttpRequest request,
        string bidPackageId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToInvite.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var command = await request.ReadFromJsonAsync<SendBidPackageInvite>();
        if (command is null) return new BadRequestResult();
        if (command.BidPackageId != bidPackageId) return new BadRequestObjectResult("Route bidPackageId does not match body.");
        if (string.IsNullOrWhiteSpace(command.Subject)) return new BadRequestObjectResult(new[] { "Subject is required." });
        if (string.IsNullOrWhiteSpace(command.HtmlBody)) return new BadRequestObjectResult(new[] { "The message body is required." });

        return new OkObjectResult(await send.HandleAsync(command, cancellationToken));
    }
}
