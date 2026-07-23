using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Subcontractors.Documents;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

/// <summary>
/// GET /api/subcontractors/{subcontractorId}/statement/pdf — renders and streams the
/// subcontractor's statement of account. Regenerated from the register on every download, so it
/// always reflects the orders and invoice links as they stand; nothing is stored. The email
/// command attaches the same rendering, so what's downloaded and what's sent never diverge.
/// </summary>
public sealed class DownloadSubcontractorStatementPdfEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> handler;

    public DownloadSubcontractorStatementPdfEndpoint(
        SignedInUserResolver users,
        IQueryHandler<GetSubcontractorStatement, SubcontractorStatement> handler)
    {
        this.users = users;
        this.handler = handler;
    }

    [Function(nameof(DownloadSubcontractorStatementPdfEndpoint))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "subcontractors/{subcontractorId}/statement/pdf")] HttpRequest request,
        string subcontractorId)
    {
        var cancellationToken = request.HttpContext.RequestAborted;

        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!GetSubcontractorStatementEndpoint.RolesThatMayReadStatements.IncludesAny(signedInUser.Roles))
            return new StatusCodeResult(403);

        SubcontractorStatement statement;
        try
        {
            statement = await handler.HandleAsync(new GetSubcontractorStatement(subcontractorId), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return new NotFoundObjectResult(ex.Message);
        }

        var pdf = SubcontractorStatementRenderer.Render(statement);

        var fileName = SanitiseFileName(
            $"{statement.CompanyName} - Statement of account - {statement.GeneratedAt:yyyy-MM-dd}.pdf");
        return new FileContentResult(pdf, "application/pdf") { FileDownloadName = fileName };
    }

    private static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
