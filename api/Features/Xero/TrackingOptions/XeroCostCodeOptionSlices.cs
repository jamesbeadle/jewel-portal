using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.TrackingOptions;

/// <summary>
/// Xero's "Cost Code" tracking category vs the portal's cost-code master (2026-09-03 — see
/// contracts/Xero/XeroCostCodeOptions.cs for the story and the decision). One slice: the gap read.
/// The portal never writes tracking options — a person creates them in Xero from this list. Gated
/// like the Cost codes page itself (Director / Finance Director / Estimator; Admin expands to every
/// role at resolution), the roles that manage the master.
/// </summary>
internal static class XeroCostCodeOptionRoles
{
    // Replica of AddCostCenterAuthorisation.RolesThatMayManageCostCenters — whoever may add a
    // code to the master may see what Xero is missing.
    public static readonly RoleSet ManageCostCodeOptions =
        RoleSet.Of(JpmsRoles.Director, JpmsRoles.FinanceDirector, JpmsRoles.Estimator);
}

// ── the gap read ───────────────────────────────────────────────────────────────────────────

public sealed class GetXeroCostCodeOptionGapsHandler : IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public GetXeroCostCodeOptionGapsHandler(JpmsContext context, IXeroClient xero)
    { this.context = context; this.xero = xero; }

    public async Task<XeroCostCodeOptionGaps> HandleAsync(GetXeroCostCodeOptionGaps query, CancellationToken cancellationToken)
    {
        if (!xero.IsConfigured) return XeroCostCodeOptionGaps.NotConfigured();

        // Always a fresh read: this is the list someone is about to act on.
        var snapshot = await xero.GetTrackingCategoriesSnapshotAsync(force: true, cancellationToken);
        if (!snapshot.IsConfigured) return XeroCostCodeOptionGaps.NotConfigured();
        if (snapshot.Error is not null) return XeroCostCodeOptionGaps.Failed(snapshot.Error);

        var category = snapshot.Categories.FirstOrDefault(c => c.IsCostCodeCategory);
        if (category is null)
            return XeroCostCodeOptionGaps.Failed("Xero has no tracking category matching the configured Cost Code name.");

        var codes = await XeroCostCodeOptionNames.ActiveCodesAsync(context, cancellationToken);

        var active = category.Options.Where(o => !o.IsArchived)
            .ToDictionary(o => o.Name.Trim(), o => o, StringComparer.OrdinalIgnoreCase);
        var archived = category.Options.Where(o => o.IsArchived)
            .ToDictionary(o => o.Name.Trim(), o => o, StringComparer.OrdinalIgnoreCase);

        var missing = new List<XeroCostCodeOptionGap>();
        var archivedGaps = new List<XeroCostCodeOptionGap>();
        var present = new List<XeroCostCodeOptionGap>();
        var matchedOptionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
        {
            if (active.ContainsKey(code.OptionName)) { present.Add(code); matchedOptionNames.Add(code.OptionName); }
            else if (archived.ContainsKey(code.OptionName)) { archivedGaps.Add(code); matchedOptionNames.Add(code.OptionName); }
            else missing.Add(code);
        }

        var xeroOnly = category.Options
            .Where(o => !o.IsArchived && !matchedOptionNames.Contains(o.Name.Trim()))
            .Select(o => o.Name)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new XeroCostCodeOptionGaps(
            true, null, category.Name, active.Count, archived.Count,
            missing, archivedGaps, present, xeroOnly);
    }
}

/// <summary>
/// Which option name each active portal code codes under: its current Xero mapping's tracking
/// option when one is set, else the code itself — exactly the coding run's rule
/// ("a cost code with a blank tracking option codes under its own code name").
/// </summary>
internal static class XeroCostCodeOptionNames
{
    public static async Task<IReadOnlyList<XeroCostCodeOptionGap>> ActiveCodesAsync(JpmsContext context, CancellationToken ct)
    {
        var codes = await context.CostCenters.AsNoTracking()
            .Where(row => row.IsActive)
            .OrderBy(row => row.SortOrder).ThenBy(row => row.Code)
            .Select(row => new { row.Code, row.Name })
            .ToListAsync(ct);

        var mappings = (await context.CostCodeXeroMappings.AsNoTracking()
                .Where(row => row.EffectiveTo == null)
                .Select(row => new { row.CostCode, row.XeroTrackingOptionName })
                .ToListAsync(ct))
            .Where(row => !string.IsNullOrWhiteSpace(row.XeroTrackingOptionName))
            .GroupBy(row => row.CostCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().XeroTrackingOptionName.Trim(), StringComparer.OrdinalIgnoreCase);

        return codes
            .Select(code => new XeroCostCodeOptionGap(
                code.Code, code.Name,
                mappings.TryGetValue(code.Code.Trim(), out var mapped) ? mapped : code.Code.Trim()))
            .ToList();
    }
}

// ── HTTP endpoints ────────────────────────────────────────────────────────────────────────

public sealed class GetXeroCostCodeOptionGapsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps> handler;
    public GetXeroCostCodeOptionGapsEndpoint(SignedInUserResolver users, IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(GetXeroCostCodeOptionGaps))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "xero/tracking-options/cost-codes/gaps")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!XeroCostCodeOptionRoles.ManageCostCodeOptions.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        return new OkObjectResult(await handler.HandleAsync(new GetXeroCostCodeOptionGaps(), request.HttpContext.RequestAborted));
    }
}
