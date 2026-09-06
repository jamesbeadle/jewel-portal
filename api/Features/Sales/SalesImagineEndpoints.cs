using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Sales.Commands;
using Jewel.JPMS.Api.Features.Sales.Imagine;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Api.Features.Sales;

/// <summary>
/// The staff side of the post-identification journey, on the lead: issue the imagine link,
/// retry a failed render, read a render (the lead page's gallery streams through here, signed
/// in), and the proposal writes. Same gate shape as SalesLeadEndpoints. The prospect's side is
/// ImaginePublicEndpoints.
/// </summary>
public sealed class SalesImagineEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly JpmsContext context;
    private readonly IImagineImageStore store;
    private readonly IssueImagineLinkAuthorisation issueAuthorisation;
    private readonly IssueImagineLinkValidation issueValidation;
    private readonly ICommandHandler<IssueImagineLink, Lead> issue;
    private readonly RetryImagineRoundAuthorisation retryAuthorisation;
    private readonly RetryImagineRoundValidation retryValidation;
    private readonly ICommandHandler<RetryImagineRound, ImagineRoundView> retry;
    private readonly SaveSalesProposalAuthorisation saveAuthorisation;
    private readonly SaveSalesProposalValidation saveValidation;
    private readonly ICommandHandler<SaveSalesProposal, SalesProposal> save;
    private readonly SendSalesProposalAuthorisation sendAuthorisation;
    private readonly SendSalesProposalValidation sendValidation;
    private readonly ICommandHandler<SendSalesProposal, SalesProposal> send;
    private readonly WithdrawSalesProposalAuthorisation withdrawAuthorisation;
    private readonly WithdrawSalesProposalValidation withdrawValidation;
    private readonly ICommandHandler<WithdrawSalesProposal, SalesProposal> withdraw;

    public SalesImagineEndpoints(
        SignedInUserResolver users,
        AuditActor auditActor,
        JpmsContext context,
        IImagineImageStore store,
        IssueImagineLinkAuthorisation issueAuthorisation,
        IssueImagineLinkValidation issueValidation,
        ICommandHandler<IssueImagineLink, Lead> issue,
        RetryImagineRoundAuthorisation retryAuthorisation,
        RetryImagineRoundValidation retryValidation,
        ICommandHandler<RetryImagineRound, ImagineRoundView> retry,
        SaveSalesProposalAuthorisation saveAuthorisation,
        SaveSalesProposalValidation saveValidation,
        ICommandHandler<SaveSalesProposal, SalesProposal> save,
        SendSalesProposalAuthorisation sendAuthorisation,
        SendSalesProposalValidation sendValidation,
        ICommandHandler<SendSalesProposal, SalesProposal> send,
        WithdrawSalesProposalAuthorisation withdrawAuthorisation,
        WithdrawSalesProposalValidation withdrawValidation,
        ICommandHandler<WithdrawSalesProposal, SalesProposal> withdraw)
    {
        this.users = users; this.auditActor = auditActor; this.context = context; this.store = store;
        this.issueAuthorisation = issueAuthorisation; this.issueValidation = issueValidation; this.issue = issue;
        this.retryAuthorisation = retryAuthorisation; this.retryValidation = retryValidation; this.retry = retry;
        this.saveAuthorisation = saveAuthorisation; this.saveValidation = saveValidation; this.save = save;
        this.sendAuthorisation = sendAuthorisation; this.sendValidation = sendValidation; this.send = send;
        this.withdrawAuthorisation = withdrawAuthorisation; this.withdrawValidation = withdrawValidation; this.withdraw = withdraw;
    }

    [Function(nameof(IssueImagineLink))]
    public async Task<IActionResult> Issue(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/imagine/link")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<IssueImagineLink>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        var command = posted with { IssuedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!issueAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = issueValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => issue.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(RetryImagineRound))]
    public async Task<IActionResult> Retry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/imagine/rounds/{roundId}/retry")] HttpRequest request, string leadId, string roundId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<RetryImagineRound>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId || posted.RoundId != roundId) return new BadRequestObjectResult("Route does not match body.");
        var command = posted with { RequestedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!retryAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = retryValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => retry.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function("SalesImagineImage")]
    public async Task<IActionResult> Image(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sales/leads/{leadId}/imagine/images/{imageId}")] HttpRequest request, string leadId, string imageId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!SalesRoles.Readers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var image = await context.ImagineImages.AsNoTracking()
            .FirstOrDefaultAsync(row => row.ImageId == imageId && row.LeadId == leadId, request.HttpContext.RequestAborted);
        if (image is null) return new NotFoundResult();
        var blob = await store.OpenAsync(image.BlobRef, request.HttpContext.RequestAborted);
        if (blob is null) return new NotFoundResult();
        request.HttpContext.Response.Headers["Cache-Control"] = "private, max-age=86400";
        return new FileStreamResult(blob.Content, blob.ContentType);
    }

    [Function(nameof(SaveSalesProposal))]
    public async Task<IActionResult> Save(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/proposals")] HttpRequest request, string leadId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SaveSalesProposal>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId) return new BadRequestObjectResult("Route leadId does not match body.");
        var command = posted with { SavedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!saveAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = saveValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => save.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(SendSalesProposal))]
    public async Task<IActionResult> Send(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/proposals/{proposalId}/send")] HttpRequest request, string leadId, string proposalId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<SendSalesProposal>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId || posted.ProposalId != proposalId) return new BadRequestObjectResult("Route does not match body.");
        var command = posted with { SentByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!sendAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = sendValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => send.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    [Function(nameof(WithdrawSalesProposal))]
    public async Task<IActionResult> Withdraw(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sales/leads/{leadId}/proposals/{proposalId}/withdraw")] HttpRequest request, string leadId, string proposalId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<WithdrawSalesProposal>();
        if (posted is null) return new BadRequestResult();
        if (posted.LeadId != leadId || posted.ProposalId != proposalId) return new BadRequestObjectResult("Route does not match body.");
        var command = posted with { DecidedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!withdrawAuthorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = withdrawValidation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        return await Run(() => withdraw.HandleAsync(command, request.HttpContext.RequestAborted));
    }

    private static async Task<IActionResult> Run<T>(Func<Task<T>> handle)
    {
        try { return new OkObjectResult(await handle()); }
        catch (InvalidOperationException ex) { return new BadRequestObjectResult(ex.Message); }
    }
}
