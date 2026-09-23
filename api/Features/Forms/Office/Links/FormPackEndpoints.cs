using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>Issuing a new starter's pack, chasing it and cancelling it — office sign-in only.</summary>
public sealed class FormPackEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly SendFormPackAuthorisation sendAuthorisation;
    private readonly SendFormPackValidation sendValidation;
    private readonly ICommandHandler<SendFormPack, SentFormPack> send;
    private readonly ChaseFormPackAuthorisation chaseAuthorisation;
    private readonly ChaseFormPackValidation chaseValidation;
    private readonly ICommandHandler<ChaseFormPack, SentFormPack> chase;
    private readonly CancelFormPackAuthorisation cancelAuthorisation;
    private readonly CancelFormPackValidation cancelValidation;
    private readonly ICommandHandler<CancelFormPack, FormPack> cancel;

    public FormPackEndpoints(
        SignedInUserResolver users,
        SendFormPackAuthorisation sendAuthorisation, SendFormPackValidation sendValidation,
        ICommandHandler<SendFormPack, SentFormPack> send,
        ChaseFormPackAuthorisation chaseAuthorisation, ChaseFormPackValidation chaseValidation,
        ICommandHandler<ChaseFormPack, SentFormPack> chase,
        CancelFormPackAuthorisation cancelAuthorisation, CancelFormPackValidation cancelValidation,
        ICommandHandler<CancelFormPack, FormPack> cancel)
    {
        this.users = users;
        this.sendAuthorisation = sendAuthorisation;
        this.sendValidation = sendValidation;
        this.send = send;
        this.chaseAuthorisation = chaseAuthorisation;
        this.chaseValidation = chaseValidation;
        this.chase = chase;
        this.cancelAuthorisation = cancelAuthorisation;
        this.cancelValidation = cancelValidation;
        this.cancel = cancel;
    }

    [Function(nameof(SendFormPack))]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-packs")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SendFormPack>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say who the pack is for.");
        var command = posted with { SentByEmail = signedInUser.Email, SentByName = signedInUser.DisplayName };
        if (!sendAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(send, command, sendValidation.Check(command), cancellationToken);
    }

    [Function(nameof(ChaseFormPack))]
    public async Task<IActionResult> Chase(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-packs/{formPackId}/chase")] HttpRequest request,
        string formPackId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new ChaseFormPack(formPackId, signedInUser.Email, signedInUser.DisplayName);
        if (!chaseAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(chase, command, chaseValidation.Check(command), cancellationToken);
    }

    [Function(nameof(CancelFormPack))]
    public async Task<IActionResult> Cancel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-packs/{formPackId}/cancel")] HttpRequest request,
        string formPackId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new CancelFormPack(formPackId);
        if (!cancelAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(cancel, command, cancelValidation.Check(command), cancellationToken);
    }
}
