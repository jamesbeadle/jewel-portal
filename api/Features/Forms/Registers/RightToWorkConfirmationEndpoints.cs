using Jewel.JPMS.Api.Features.Forms.Office;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>The register's "Send confirmation": a completed pass emailed to the person it is about — right-to-work readers only.</summary>
public sealed class RightToWorkConfirmationEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly SendRightToWorkConfirmationAuthorisation confirmAuthorisation;
    private readonly SendRightToWorkConfirmationValidation confirmValidation;
    private readonly ICommandHandler<SendRightToWorkConfirmation, RightToWorkCheck> confirm;

    public RightToWorkConfirmationEndpoints(
        SignedInUserResolver users, SendRightToWorkConfirmationAuthorisation confirmAuthorisation,
        SendRightToWorkConfirmationValidation confirmValidation, ICommandHandler<SendRightToWorkConfirmation, RightToWorkCheck> confirm)
    {
        this.users = users;
        this.confirmAuthorisation = confirmAuthorisation;
        this.confirmValidation = confirmValidation;
        this.confirm = confirm;
    }

    [Function(nameof(SendRightToWorkConfirmation))]
    public async Task<IActionResult> Confirm(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "right-to-work-checks/{rightToWorkCheckId}/confirmation")] HttpRequest request,
        string rightToWorkCheckId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new SendRightToWorkConfirmation(rightToWorkCheckId, signedInUser.Email);
        if (!confirmAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(confirm, command, confirmValidation.Check(command), cancellationToken);
    }
}
