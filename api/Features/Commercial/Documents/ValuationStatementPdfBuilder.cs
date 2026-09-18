using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Documents;

/// <summary>
/// The rendered statement PDF plus the identity the callers need around it: the filename it
/// should travel under and the project the valuation belongs to (for authorisation and
/// addressing), and the claim it printed.
/// </summary>
public sealed record ValuationStatementPdf(
    byte[] Content,
    string FileName,
    string ProjectId,
    string ProjectName,
    ValuationClaim Claim);

/// <summary>
/// One valuation statement, loaded and ready to render — a locked claim's frozen rows, or the
/// working copy of a Draft — with everything around it that both documents share: the project
/// identity for the header, the cost-centre names for the bill's area sub-headings, and the ONE
/// file-name stem the PDF and the spreadsheet travel under (<see cref="ValuationReportFileNames"/>:
/// extension aside, the pair share a name exactly). Load it once and render both files from it,
/// so a PDF and a workbook exported together can never disagree about what the report said (the
/// connector's export_valuation_report does this).
/// </summary>
public sealed record ValuationReportStatement(
    string ProjectId,
    string ProjectReference,
    string ProjectName,
    string ClientName,
    ValuationStatement Statement,
    bool IsDraft,
    string FileNameStem,
    IReadOnlyDictionary<string, string> CostCentreNames);

/// <summary>
/// Assembles and renders the statement PDF in one place — the download endpoint streams exactly
/// the bytes the email command attaches, so what's downloaded and what's sent never diverge.
/// Loads the statement through the one query handler (which also resolves retired snapshot ids)
/// and the project header for the document's identity, then hands both to
/// <see cref="ValuationStatementRenderer"/>. A Draft claim renders as the working copy with its
/// stamps; a locked one as the issued record. <see cref="BuildDraftAsync"/> is the same for the
/// project's LATEST claim, whatever its status. The loading half is public on its own
/// (<see cref="LoadAsync"/> / <see cref="LoadDraftAsync"/>) so the spreadsheet builder can
/// render the same loaded statement.
/// </summary>
public sealed class ValuationStatementPdfBuilder
{
    private readonly IQueryHandler<GetValuationStatement, ValuationStatement> statements;
    private readonly JpmsContext context;

    public ValuationStatementPdfBuilder(
        IQueryHandler<GetValuationStatement, ValuationStatement> statements,
        JpmsContext context)
    {
        this.statements = statements; this.context = context;
    }

    public async Task<ValuationStatementPdf> BuildAsync(string claimId, CancellationToken cancellationToken) =>
        Render(await LoadAsync(claimId, cancellationToken));

    /// <summary>The project's latest valuation as its statement — the working copy while Draft.</summary>
    public async Task<ValuationStatementPdf> BuildDraftAsync(string projectId, CancellationToken cancellationToken) =>
        Render(await LoadDraftAsync(projectId, cancellationToken));

    /// <summary>A valuation's statement with its project, ready to render.</summary>
    public async Task<ValuationReportStatement> LoadAsync(string claimId, CancellationToken cancellationToken)
    {
        var statement = await statements.HandleAsync(new GetValuationStatement(claimId), cancellationToken);
        return await WrapAsync(statement, cancellationToken);
    }

    /// <summary>The latest claim's statement — the working copy while it is a Draft.</summary>
    public async Task<ValuationReportStatement> LoadDraftAsync(string projectId, CancellationToken cancellationToken)
    {
        var claim = await context.ValuationClaims.AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .OrderByDescending(c => c.ClaimNumber)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("This project has no valuation claim yet — start one on the Valuation Report tab.");
        return await WrapAsync(await ValuationStatementLines.ReadAsync(context, claim, cancellationToken), cancellationToken);
    }

    private async Task<ValuationReportStatement> WrapAsync(ValuationStatement statement, CancellationToken cancellationToken)
    {
        var claim = statement.Claim;
        var project = await context.Projects.FindAsync(new object[] { claim.ProjectId }, cancellationToken)
            ?? throw new InvalidOperationException($"Project {claim.ProjectId} for valuation {claim.ValuationClaimId} not found.");

        // Named by the claim alone — the working-copy wording stays inside the document — so the
        // PDF, the page's spreadsheet and the emailed attachment of one claim share a name.
        return new ValuationReportStatement(
            project.ProjectId, project.Reference, project.Name, project.ClientName, statement, statement.IsDraft,
            ValuationReportFileNames.For(project.Reference, project.Name, claim.DisplayName, statement.AsAt),
            await CostCentreNamesAsync(cancellationToken));
    }

    /// <summary>Renders a loaded statement — frozen or working copy — to its PDF.</summary>
    public static ValuationStatementPdf Render(ValuationReportStatement statement)
    {
        var pdf = ValuationStatementRenderer.Render(new ValuationStatementDocument(
            statement.ProjectReference,
            statement.ProjectName,
            statement.ClientName,
            statement.Statement,
            IsDraft: statement.IsDraft,
            CostCentreNames: statement.CostCentreNames));

        return new ValuationStatementPdf(
            pdf, SanitiseFileName($"{statement.FileNameStem}.pdf"),
            statement.ProjectId, statement.ProjectName, statement.Statement.Claim);
    }

    // Cost code → master name, for the bill's area sub-headings when a line carries no
    // estimate section (the ValuationReportAreas rule the renderer applies). Grouped rather
    // than ToDictionary so a duplicated code can never turn a PDF download into a 500.
    private async Task<IReadOnlyDictionary<string, string>> CostCentreNamesAsync(CancellationToken cancellationToken)
    {
        var centres = await context.CostCenters.AsNoTracking()
            .Select(centre => new { centre.Code, centre.Name })
            .ToListAsync(cancellationToken);
        return centres
            .GroupBy(centre => centre.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.OrdinalIgnoreCase);
    }

    internal static string SanitiseFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(fileName.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
