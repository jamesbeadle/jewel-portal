using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs.Audits.Commands;

/// <summary>The audit's HTTP surface: planted on the project route; edited, issued and closed on
/// its own route — the building-control shape. The creator is always the signed-in user.</summary>
public sealed class HsAuditEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly CreateHsAuditAuthorisation createAuthorisation;
    private readonly CreateHsAuditValidation createValidation;
    private readonly ICommandHandler<CreateHsAudit, HsAudit> create;
    private readonly UpdateHsAuditDetailsAuthorisation detailsAuthorisation;
    private readonly UpdateHsAuditDetailsValidation detailsValidation;
    private readonly ICommandHandler<UpdateHsAuditDetails, HsAudit> updateDetails;
    private readonly UpdateHsAuditItemsAuthorisation itemsAuthorisation;
    private readonly UpdateHsAuditItemsValidation itemsValidation;
    private readonly ICommandHandler<UpdateHsAuditItems, HsAuditView> updateItems;
    private readonly IssueHsAuditAuthorisation issueAuthorisation;
    private readonly ICommandHandler<IssueHsAudit, HsAuditView> issue;
    private readonly CloseHsAuditAuthorisation closeAuthorisation;
    private readonly CloseHsAuditValidation closeValidation;
    private readonly ICommandHandler<CloseHsAudit, HsAudit> close;

    public HsAuditEndpoints(
        SignedInUserResolver users,
        CreateHsAuditAuthorisation createAuthorisation,
        CreateHsAuditValidation createValidation,
        ICommandHandler<CreateHsAudit, HsAudit> create,
        UpdateHsAuditDetailsAuthorisation detailsAuthorisation,
        UpdateHsAuditDetailsValidation detailsValidation,
        ICommandHandler<UpdateHsAuditDetails, HsAudit> updateDetails,
        UpdateHsAuditItemsAuthorisation itemsAuthorisation,
        UpdateHsAuditItemsValidation itemsValidation,
        ICommandHandler<UpdateHsAuditItems, HsAuditView> updateItems,
        IssueHsAuditAuthorisation issueAuthorisation,
        ICommandHandler<IssueHsAudit, HsAuditView> issue,
        CloseHsAuditAuthorisation closeAuthorisation,
        CloseHsAuditValidation closeValidation,
        ICommandHandler<CloseHsAudit, HsAudit> close)
    {
        this.users = users;
        this.createAuthorisation = createAuthorisation;
        this.createValidation = createValidation;
        this.create = create;
        this.detailsAuthorisation = detailsAuthorisation;
        this.detailsValidation = detailsValidation;
        this.updateDetails = updateDetails;
        this.itemsAuthorisation = itemsAuthorisation;
        this.itemsValidation = itemsValidation;
        this.updateItems = updateItems;
        this.issueAuthorisation = issueAuthorisation;
        this.issue = issue;
        this.closeAuthorisation = closeAuthorisation;
        this.closeValidation = closeValidation;
        this.close = close;
    }

    [Function(nameof(CreateHsAudit))]
    public async Task<IActionResult> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/hs-audits")] HttpRequest request,
        string projectId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateHsAudit>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An audit body is required.");
        var command = posted with { ProjectId = projectId, CreatedByEmail = signedInUser.Email };

        if (!createAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = createValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await create.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(UpdateHsAuditDetails))]
    public async Task<IActionResult> UpdateDetails(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "hs-audits/{auditId}")] HttpRequest request,
        string auditId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateHsAuditDetails>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An audit body is required.");
        var command = posted with { HsAuditId = auditId };

        if (!detailsAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = detailsValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await updateDetails.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(UpdateHsAuditItems))]
    public async Task<IActionResult> UpdateItems(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "hs-audits/{auditId}/items")] HttpRequest request,
        string auditId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<UpdateHsAuditItems>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("An items body is required.");
        var command = posted with { HsAuditId = auditId };

        if (!itemsAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = itemsValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await updateItems.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(IssueHsAudit))]
    public async Task<IActionResult> Issue(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hs-audits/{auditId}/issue")] HttpRequest request,
        string auditId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var command = new IssueHsAudit(auditId, signedInUser.Email);
        if (!issueAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await issue.HandleAsync(command, cancellationToken));
    }

    [Function(nameof(CloseHsAudit))]
    public async Task<IActionResult> Close(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hs-audits/{auditId}/close")] HttpRequest request,
        string auditId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CloseHsAudit>(cancellationToken);
        if (posted is null) return new BadRequestObjectResult("A close body is required.");
        var command = posted with { HsAuditId = auditId };

        if (!closeAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = closeValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        return new OkObjectResult(await close.HandleAsync(command, cancellationToken));
    }
}
