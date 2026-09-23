using Jewel.JPMS.Api.Features.Forms.Office;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>The right-to-work register: recording a check, filing the checker's evidence, and reading the register — its readers only.</summary>
public sealed class RightToWorkEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly SaveRightToWorkCheckAuthorisation saveAuthorisation;
    private readonly SaveRightToWorkCheckValidation saveValidation;
    private readonly ICommandHandler<SaveRightToWorkCheck, RightToWorkCheck> save;
    private readonly RightToWorkEvidenceFiling evidence;
    private readonly IQueryHandler<ListRightToWorkChecks, IReadOnlyList<RightToWorkCheck>> list;

    public RightToWorkEndpoints(
        SignedInUserResolver users, SaveRightToWorkCheckAuthorisation saveAuthorisation, SaveRightToWorkCheckValidation saveValidation,
        ICommandHandler<SaveRightToWorkCheck, RightToWorkCheck> save, RightToWorkEvidenceFiling evidence,
        IQueryHandler<ListRightToWorkChecks, IReadOnlyList<RightToWorkCheck>> list)
    {
        this.users = users;
        this.saveAuthorisation = saveAuthorisation;
        this.saveValidation = saveValidation;
        this.save = save;
        this.evidence = evidence;
        this.list = list;
    }

    [Function(nameof(SaveRightToWorkCheck))]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "right-to-work-checks")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SaveRightToWorkCheck>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("The check's details are required.");
        var command = posted with { RecordedByEmail = signedInUser.Email };
        if (!saveAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.RunAsync(save, command, saveValidation.Check(command), cancellationToken);
    }

    [Function("UploadRightToWorkEvidence")]
    public async Task<IActionResult> UploadEvidence(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "right-to-work-checks/{rightToWorkCheckId}/evidence")] HttpRequest request,
        string rightToWorkCheckId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.RightToWorkReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected a file.");
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();
        if (file is null) return new BadRequestObjectResult("Choose the evidence file.");
        try { return new OkObjectResult(await evidence.FileAsync(rightToWorkCheckId, file, cancellationToken)); }
        catch (InvalidOperationException refusal) { return new ConflictObjectResult(refusal.Message); }
    }

    [Function(nameof(ListRightToWorkChecks))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "right-to-work-checks")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!FormRoleSets.RightToWorkReaders.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return await OfficeAnswers.ReadAsync(list, new ListRightToWorkChecks(), request.HttpContext.RequestAborted);
    }
}
