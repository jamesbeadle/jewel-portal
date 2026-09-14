using Jewel.JPMS.Api.Features.Commercial.Documents;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Queries;

/// <summary>
/// GET projects/{projectId}/suppliers/{subcontractorId}/account/pdf — the supplier's account on
/// the project as a branded PDF, the sheet the finance director sends the managing director when
/// a supplier has invoiced past their orders. A plain GET so the modal can link to it directly
/// (the session cookie authenticates, as with the statement and reconciliation PDFs); rendered
/// from the register on every download, nothing stored.
/// </summary>
public sealed class DownloadProjectSupplierAccountPdfEndpoint
{
    private const string PdfContentType = "application/pdf";

    private static readonly RoleSet InternalReadRoles = JpmsRoleSets.AllInternal;

    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetProjectSupplierAccount, ProjectSupplierAccount> handler;

    public DownloadProjectSupplierAccountPdfEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetProjectSupplierAccount, ProjectSupplierAccount> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(DownloadProjectSupplierAccountPdfEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/suppliers/{subcontractorId}/account/pdf")] HttpRequest request,
        string projectId,
        string subcontractorId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!InternalReadRoles.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);

        ProjectSupplierAccount account;
        try
        {
            account = await handler.HandleAsync(new GetProjectSupplierAccount(projectId, subcontractorId), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }

        var pdf = ProjectSupplierAccountRenderer.Render(account);
        var fileName = ProjectSupplierAccountFileNames.For(
            account.ProjectReference, account.ProjectName, account.SupplierName, account.GeneratedAt) + ".pdf";
        return new FileContentResult(pdf, PdfContentType) { FileDownloadName = fileName };
    }
}
