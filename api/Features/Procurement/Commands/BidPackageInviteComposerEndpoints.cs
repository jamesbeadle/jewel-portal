using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// The in-app invite composer's three endpoints: read the persisted draft, save it, send the
/// invite. Sending an invite (and drafting one) is exactly the set who could create the draft in
/// the old Outlook flow — SendBidPackageInviteToTenderListAuthorisation's roles — reused so moving
/// the send in-app widened nobody's reach.
/// </summary>
public sealed class BidPackageInviteComposerEndpoints
{
    // One home for the set: the send's own authorisation class, which the connector's action
    // reads too. The draft's read and save borrow it — seeing the composer and sending from it
    // are the same permission.
    private static readonly RoleSet AllowedToInvite = SendBidPackageInviteAuthorisation.RolesThatMayInvite;

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?> get;
    private readonly ICommandHandler<SaveBidPackageInviteComposerDraft, Acknowledgement> save;
    private readonly ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome> send;
    private readonly SendBidPackageInviteValidation validation;

    public BidPackageInviteComposerEndpoints(
        SignedInUserResolver users,
        IQueryHandler<GetBidPackageInviteComposerDraft, BidPackageInviteComposerDraft?> get,
        ICommandHandler<SaveBidPackageInviteComposerDraft, Acknowledgement> save,
        ICommandHandler<SendBidPackageInvite, BidPackageInviteSendOutcome> send,
        SendBidPackageInviteValidation validation)
    {
        this.users = users; this.get = get; this.save = save; this.send = send; this.validation = validation;
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

        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        return new OkObjectResult(await send.HandleAsync(command, cancellationToken));
    }
}
