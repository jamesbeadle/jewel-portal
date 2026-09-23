using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Thread.Commands;

/// <summary>POST /api/hs-records/{hsRecordId}/comments — a comment from words alone, the author stamped from the sign-in.</summary>
public sealed class CommentOnHsRecordEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly CommentOnHsRecordAuthorisation authorisation;
    private readonly CommentOnHsRecordValidation validation;
    private readonly ICommandHandler<CommentOnHsRecord, HsRecordComment> handler;

    public CommentOnHsRecordEndpoint(
        SignedInUserResolver users, CommentOnHsRecordAuthorisation authorisation, CommentOnHsRecordValidation validation,
        ICommandHandler<CommentOnHsRecord, HsRecordComment> handler)
    {
        this.users = users;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CommentOnHsRecord))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hs-records/{hsRecordId}/comments")] HttpRequest request, string hsRecordId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<CommentOnHsRecord>(cancellationToken);
        if (posted is null) return new BadRequestResult();
        if (posted.HsRecordId != hsRecordId) return new BadRequestObjectResult("Route hsRecordId does not match body.");
        var command = posted with { AuthorEmail = signedInUser.Email, AuthorName = signedInUser.DisplayName };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try { return new OkObjectResult(await handler.HandleAsync(command, cancellationToken)); }
        catch (InvalidOperationException refusal) { return new ConflictObjectResult(refusal.Message); }
    }
}
