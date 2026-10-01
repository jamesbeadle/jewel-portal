using Jewel.JPMS.Api.Features.Ai;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

/// <summary>GET /api/my/labour/cost-code-suggestion?projectId=…&amp;description=… — the cost code
/// the day's words point to, from the site's own list. Never an error: no match, no AI key or a
/// failed call is an empty suggestion and the picker stays as it was.</summary>
public sealed class SuggestMyDayCostCodeEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SuggestMyDayCostCodeHandler handler;
    public SuggestMyDayCostCodeEndpoint(SignedInUserResolver users, SuggestMyDayCostCodeHandler handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SuggestMyDayCostCode))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my/labour/cost-code-suggestion")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.LogOwnTime.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var projectId = request.Query["projectId"].ToString();
        var description = request.Query["description"].ToString();
        if (string.IsNullOrWhiteSpace(projectId)) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(new SuggestMyDayCostCode(projectId, description), cancellationToken));
    }
}

public sealed class SuggestMyDayCostCodeHandler : IQueryHandler<SuggestMyDayCostCode, MyDayCostCodeSuggestion>
{
    private readonly MyDayCostCodes costCodes;
    private readonly IClaudeClient claude;
    private readonly ILogger<SuggestMyDayCostCodeHandler> logger;

    public SuggestMyDayCostCodeHandler(MyDayCostCodes costCodes, IClaudeClient claude, ILogger<SuggestMyDayCostCodeHandler> logger)
    { this.costCodes = costCodes; this.claude = claude; this.logger = logger; }

    public async Task<MyDayCostCodeSuggestion> HandleAsync(SuggestMyDayCostCode query, CancellationToken cancellationToken)
    {
        var hasWords = !string.IsNullOrWhiteSpace(query.Description);
        if (!hasWords) return new MyDayCostCodeSuggestion();
        var byProject = await costCodes.ByProjectAsync(new[] { query.ProjectId }, cancellationToken);
        var candidates = byProject[query.ProjectId];
        var fromRules = FromRules(query.Description, candidates);
        if (fromRules is not null) return fromRules;
        return await FromClaudeAsync(query.Description, candidates, cancellationToken) ?? new MyDayCostCodeSuggestion();
    }

    private static MyDayCostCodeSuggestion? FromRules(string description, IReadOnlyList<SiteSheetCostCode> candidates)
    {
        var codes = candidates.Select(candidate => candidate.Code).ToList();
        var matched = ProgrammeCostCentreRules.Match(description, codes);
        var isOneCode = matched.Count == 1;
        if (!isOneCode) return null;
        var code = candidates.First(candidate => string.Equals(candidate.Code, matched[0], StringComparison.OrdinalIgnoreCase));
        return new MyDayCostCodeSuggestion(code.Code, code.Name, MyDaySuggestionSource.Rule);
    }

    private async Task<MyDayCostCodeSuggestion?> FromClaudeAsync(string description, IReadOnlyList<SiteSheetCostCode> candidates, CancellationToken cancellationToken)
    {
        if (!claude.IsConfigured || candidates.Count == 0) return null;
        var response = await claude.CompleteAsync(MyDayCostCodePrompt.System, MyDayCostCodePrompt.User(description, candidates), cancellationToken);
        var answer = MyDayCostCodePrompt.ParseCode(response);
        var code = candidates.FirstOrDefault(candidate => string.Equals(candidate.Code, answer, StringComparison.OrdinalIgnoreCase));
        if (code is null)
        {
            logger.LogInformation("No cost code suggested from the day's words ({Answer}).", answer);
            return null;
        }
        return new MyDayCostCodeSuggestion(code.Code, code.Name, MyDaySuggestionSource.Claude);
    }
}
