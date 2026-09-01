using Jewel.JPMS.Api.Gates;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Procurement.Attachments;

/// <summary>
/// The company's standard tender Terms &amp; Conditions PDF — one document, company-wide, attached
/// automatically to every tender invite. Reads open to the whole internal team (anyone drafting an
/// invite can see what will travel with it); uploading a replacement is Admin/Director only,
/// because it changes what every project sends out from that moment on.
/// </summary>
public sealed class CompanyTenderTermsEndpoints
{
    // A terms document is a few hundred KB; 10 MB is far past any real one.
    private const long MaxTermsBytes = 10L * 1024 * 1024;

    private static readonly RoleSet AllowedToRead = JpmsRoleSets.AllInternal;
    private static readonly RoleSet AllowedToReplace = RoleSet.Of(Role.Admin, JpmsRoles.Director);

    private readonly SignedInUserResolver users;
    private readonly ICompanyTenderTermsStore store;

    public CompanyTenderTermsEndpoints(SignedInUserResolver users, ICompanyTenderTermsStore store)
    {
        this.users = users;
        this.store = store;
    }

    /// <summary>GET /api/company/tender-terms — what is uploaded right now (exists=false when
    /// nothing is, or when no storage is configured; Configured says which).</summary>
    [Function(nameof(GetCompanyTenderTermsInfo))]
    public async Task<IActionResult> GetCompanyTenderTermsInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "company/tender-terms")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var info = await store.GetInfoAsync(cancellationToken);
        return new OkObjectResult(new
        {
            exists = info is not null,
            configured = store.IsConfigured,
            fileName = info?.FileName,
            fileSizeBytes = info?.FileSizeBytes ?? 0,
            uploadedAt = info?.UploadedAt
        });
    }

    /// <summary>GET /api/company/tender-terms/file — downloads the stored PDF.</summary>
    [Function(nameof(DownloadCompanyTenderTerms))]
    public async Task<IActionResult> DownloadCompanyTenderTerms(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "company/tender-terms/file")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToRead.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        var file = await store.OpenAsync(cancellationToken);
        if (file is null) return new NotFoundObjectResult("No terms document is uploaded.");

        return new FileContentResult(file.Content, "application/pdf") { FileDownloadName = file.FileName };
    }

    /// <summary>POST /api/company/tender-terms — multipart/form-data, one PDF. Replaces whatever
    /// was there; every invite drafted from now on attaches the new document.</summary>
    [Function(nameof(UploadCompanyTenderTerms))]
    public async Task<IActionResult> UploadCompanyTenderTerms(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "company/tender-terms")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!AllowedToReplace.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        if (!request.HasFormContentType) return new BadRequestObjectResult("Expected multipart/form-data.");
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault(candidate => candidate.Length > 0);
        if (file is null) return new BadRequestObjectResult("A non-empty PDF is required.");
        if (file.Length > MaxTermsBytes)
            return new BadRequestObjectResult("That file is too large — the terms document is limited to 10 MB.");

        var looksLikePdf = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!looksLikePdf)
            return new BadRequestObjectResult("The terms document must be a PDF — it travels to subcontractors as one.");

        await using var stream = file.OpenReadStream();
        var info = await store.SaveAsync(file.FileName, stream, cancellationToken);
        return new OkObjectResult(new
        {
            exists = true,
            configured = store.IsConfigured,
            fileName = info.FileName,
            fileSizeBytes = info.FileSizeBytes,
            uploadedAt = info.UploadedAt
        });
    }
}
