using Jewel.JPMS.Api.Features.Forms.Storage;
using Jewel.JPMS.Contracts.Registers;

namespace Jewel.JPMS.Api.Features.Registers.Policies;

/// <summary>
/// A policy revision's PDF on the Policies page: attached by the register's managers (multipart, one
/// PDF, refused once anyone has signed the revision), read by the internal team and by anyone asked to
/// sign it on their own login.
/// </summary>
public sealed class PolicyFileEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly JpmsContext context;
    private readonly IFormEvidenceStore store;
    private readonly IQueryHandler<ListPolicyDocuments, IReadOnlyList<PolicyDocument>> policies;

    public PolicyFileEndpoints(
        SignedInUserResolver users, JpmsContext context, IFormEvidenceStore store,
        IQueryHandler<ListPolicyDocuments, IReadOnlyList<PolicyDocument>> policies)
    {
        this.users = users;
        this.context = context;
        this.store = store;
        this.policies = policies;
    }

    [Function("AttachPolicyDocumentFile")]
    public async Task<IActionResult> Attach(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "registers/policies/{policyDocumentId}/file")] HttpRequest request,
        string policyDocumentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!RegisterRoleSets.ManageRegisters.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var policy = await context.PolicyDocuments.FirstOrDefaultAsync(row => row.PolicyDocumentId == policyDocumentId, cancellationToken);
        if (policy is null) return new NotFoundResult();
        var isSigned = await context.PolicySignOffs.AnyAsync(row => row.PolicyDocumentId == policyDocumentId && row.SignedAt != null, cancellationToken);
        if (isSigned) return new ConflictObjectResult("Someone has already signed this revision — publish a new revision to change its PDF.");
        if (!request.HasFormContentType) return new BadRequestObjectResult(new[] { "Attach the policy as a PDF." });
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault(candidate => candidate.Length > 0);
        var refusal = RefusalFor(file);
        if (refusal is not null) return new BadRequestObjectResult(new[] { refusal });
        using var buffer = new MemoryStream();
        await file!.CopyToAsync(buffer, cancellationToken);
        await PolicyFiles.AttachAsync(store, policy, file.FileName, buffer.ToArray(), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new OkObjectResult(await ListedAsync(policyDocumentId, cancellationToken));
    }

    [Function("DownloadPolicyDocumentFile")]
    public async Task<IActionResult> Download(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "registers/policies/{policyDocumentId}/file")] HttpRequest request,
        string policyDocumentId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        var mayRead = await MayReadAsync(signedInUser, policyDocumentId, cancellationToken);
        if (!mayRead) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var policy = await context.PolicyDocuments.AsNoTracking().FirstOrDefaultAsync(row => row.PolicyDocumentId == policyDocumentId, cancellationToken);
        var file = policy is null ? null : await PolicyFiles.OpenAsync(store, policy, cancellationToken);
        if (file is null) return new NotFoundObjectResult("No PDF is attached to this revision.");
        return new FileStreamResult(file.Content, file.ContentType) { FileDownloadName = policy!.FileName };
    }

    private static string? RefusalFor(IFormFile? file)
    {
        if (file is null) return "Attach the policy as a PDF.";
        if (file.Length > PolicyFiles.LargestBytes) return "That file is too large — a policy PDF is limited to 25 MB.";
        return PolicyFiles.IsAPdf(file.FileName, file.ContentType) ? null : "The policy must be a PDF.";
    }

    private async Task<bool> MayReadAsync(SignedInUser user, string policyDocumentId, CancellationToken cancellationToken)
    {
        if (JpmsRoleSets.AllInternal.IncludesAny(user.Roles)) return true;
        var email = user.Email.Trim().ToLowerInvariant();
        return await context.PolicySignOffs.AnyAsync(row => row.PolicyDocumentId == policyDocumentId && row.RecipientEmail == email, cancellationToken);
    }

    private async Task<PolicyDocument?> ListedAsync(string policyDocumentId, CancellationToken cancellationToken)
    {
        var all = await policies.HandleAsync(new ListPolicyDocuments(), cancellationToken);
        return all.FirstOrDefault(document => document.PolicyDocumentId == policyDocumentId);
    }
}
