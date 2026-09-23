using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Links;

/// <summary>Sending one form to one named person, sending it again, and cancelling it — office sign-in only.</summary>
public sealed class FormInviteEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly SendFormInviteAuthorisation sendAuthorisation;
    private readonly SendFormInviteValidation sendValidation;
    private readonly ICommandHandler<SendFormInvite, SentFormLink> send;
    private readonly ResendFormInviteAuthorisation resendAuthorisation;
    private readonly ResendFormInviteValidation resendValidation;
    private readonly ICommandHandler<ResendFormInvite, SentFormLink> resend;
    private readonly CancelFormInviteAuthorisation cancelAuthorisation;
    private readonly CancelFormInviteValidation cancelValidation;
    private readonly ICommandHandler<CancelFormInvite, FormInvite> cancel;

    public FormInviteEndpoints(
        SignedInUserResolver users,
        SendFormInviteAuthorisation sendAuthorisation, SendFormInviteValidation sendValidation,
        ICommandHandler<SendFormInvite, SentFormLink> send,
        ResendFormInviteAuthorisation resendAuthorisation, ResendFormInviteValidation resendValidation,
        ICommandHandler<ResendFormInvite, SentFormLink> resend,
        CancelFormInviteAuthorisation cancelAuthorisation, CancelFormInviteValidation cancelValidation,
        ICommandHandler<CancelFormInvite, FormInvite> cancel)
    {
        this.users = users;
        this.sendAuthorisation = sendAuthorisation;
        this.sendValidation = sendValidation;
        this.send = send;
        this.resendAuthorisation = resendAuthorisation;
        this.resendValidation = resendValidation;
        this.resend = resend;
        this.cancelAuthorisation = cancelAuthorisation;
        this.cancelValidation = cancelValidation;
        this.cancel = cancel;
    }

    [Function(nameof(SendFormInvite))]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-invites")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SendFormInvite>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("Say which form to send, and to whom.");
        var command = posted with { SentByEmail = signedInUser.Email, SentByName = signedInUser.DisplayName };
        if (!sendAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(send, command, sendValidation.Check(command), cancellationToken);
    }

    [Function(nameof(ResendFormInvite))]
    public async Task<IActionResult> Resend(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-invites/{formInviteId}/resend")] HttpRequest request,
        string formInviteId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<ResendFormInvite>(cancellationToken);
        var command = (posted ?? new ResendFormInvite(formInviteId, "")) with
        {
            FormInviteId = formInviteId, SentByEmail = signedInUser.Email, SentByName = signedInUser.DisplayName
        };
        if (!resendAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(resend, command, resendValidation.Check(command), cancellationToken);
    }

    [Function(nameof(CancelFormInvite))]
    public async Task<IActionResult> Cancel(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form-invites/{formInviteId}/cancel")] HttpRequest request,
        string formInviteId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new CancelFormInvite(formInviteId);
        if (!cancelAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(cancel, command, cancelValidation.Check(command), cancellationToken);
    }
}
