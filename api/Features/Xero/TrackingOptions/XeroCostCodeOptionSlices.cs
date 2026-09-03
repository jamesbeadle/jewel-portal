using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Xero.TrackingOptions;

/// <summary>
/// Keeping Xero's "Cost Code" tracking category in step with the portal's cost-code master
/// (2026-09-03 — see contracts/Xero/XeroCostCodeOptions.cs for the story). Three slices: the gap
/// read, the confirmed batch create, the rename. All gated like the Cost codes page itself
/// (Director / Finance Director / Estimator; Admin expands to every role at resolution), the
/// same roles that manage the master — Xero's options are that master's shadow.
/// </summary>
internal static class XeroCostCodeOptionRoles
{
    // Replica of AddCostCenterAuthorisation.RolesThatMayManageCostCenters — whoever may add a
    // code to the master may push it to Xero.
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

// ── create the missing options ────────────────────────────────────────────────────────────

public sealed class CreateXeroCostCodeOptionsAuthorisation
{
    public bool Allows(SignedInUser user, CreateXeroCostCodeOptions command) =>
        XeroCostCodeOptionRoles.ManageCostCodeOptions.IncludesAny(user.Roles);
}

public sealed class CreateXeroCostCodeOptionsValidation
{
    public ValidationOutcome Check(CreateXeroCostCodeOptions command)
    {
        var errors = new List<string>();
        if (command.Codes is { Count: > 0 } && command.Codes.Any(string.IsNullOrWhiteSpace))
            errors.Add("Every code must be given — blank entries are not allowed.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class CreateXeroCostCodeOptionsHandler : ICommandHandler<CreateXeroCostCodeOptions, XeroCostCodeOptionsCreateResult>
{
    private readonly IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps> gaps;
    private readonly IXeroClient xero;
    private readonly AuditTrail audit;
    private readonly ILogger<CreateXeroCostCodeOptionsHandler> logger;

    public CreateXeroCostCodeOptionsHandler(
        IQueryHandler<GetXeroCostCodeOptionGaps, XeroCostCodeOptionGaps> gaps,
        IXeroClient xero, AuditTrail audit, ILogger<CreateXeroCostCodeOptionsHandler> logger)
    { this.gaps = gaps; this.xero = xero; this.audit = audit; this.logger = logger; }

    public async Task<XeroCostCodeOptionsCreateResult> HandleAsync(CreateXeroCostCodeOptions command, CancellationToken cancellationToken)
    {
        var gap = await gaps.HandleAsync(new GetXeroCostCodeOptionGaps(), cancellationToken);
        if (!gap.IsConfigured)
            return new XeroCostCodeOptionsCreateResult(false, null, Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), 0);
        if (gap.Error is not null)
            return new XeroCostCodeOptionsCreateResult(true, gap.Error, Array.Empty<string>(), Array.Empty<string>(),
                Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), gap.ActiveOptionCount);

        // What to create: every missing code, or only the ones named — each named code sorted
        // into missing / already there / archived / not a portal code, so nothing is guessed.
        var alreadyPresent = new List<string>();
        var archivedInXero = new List<string>();
        var unknown = new List<string>();
        List<XeroCostCodeOptionGap> toCreate;
        if (command.Codes is { Count: > 0 })
        {
            toCreate = new List<XeroCostCodeOptionGap>();
            foreach (var requested in command.Codes.Select(code => code.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (gap.Missing.FirstOrDefault(g => Matches(g, requested)) is { } missing) toCreate.Add(missing);
                else if (gap.Present.Any(g => Matches(g, requested))) alreadyPresent.Add(requested);
                else if (gap.Archived.Any(g => Matches(g, requested))) archivedInXero.Add(requested);
                else unknown.Add(requested);
            }
        }
        else
        {
            toCreate = gap.Missing.ToList();
            archivedInXero.AddRange(gap.Archived.Select(g => g.Code));
        }

        // One option per distinct NAME — two codes mapped to the same option share one.
        var created = new List<string>();
        var createdNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? error = null;
        var stoppedAt = -1;
        for (var index = 0; index < toCreate.Count; index++)
        {
            var entry = toCreate[index];
            if (!createdNames.Add(entry.OptionName)) { created.Add(entry.Code); continue; }
            try
            {
                await xero.CreateCostCodeOptionAsync(entry.OptionName, cancellationToken);
                created.Add(entry.Code);
            }
            catch (XeroCallFailedException failure)
            {
                // Xero's words, verbatim — the option cap is the expected refusal and the
                // finance team resolves it in Xero, not here. Stop rather than hit it N times.
                error = $"Stopped at \"{entry.OptionName}\": {failure.Message}";
                createdNames.Remove(entry.OptionName);
                stoppedAt = index;
                logger.LogWarning("Cost Code option batch stopped at {Option}: {Error}", entry.OptionName, failure.Message);
                break;
            }
        }
        var notCreated = stoppedAt < 0
            ? new List<string>()
            : toCreate.Skip(stoppedAt).Select(entry => entry.Code).ToList();

        if (created.Count > 0)
            await audit.WriteAsync(
                AuditEventType.XeroCostCodeOptionsCreated,
                $"Created {created.Count} \"{gap.CategoryName}\" tracking option(s) in Xero: {string.Join(", ", created)}"
                + (error is null ? "" : $" — then Xero refused: {error}"),
                cancellationToken: cancellationToken);

        return new XeroCostCodeOptionsCreateResult(
            true, error, created, alreadyPresent, archivedInXero, unknown, notCreated,
            gap.ActiveOptionCount + createdNames.Count);
    }

    private static bool Matches(XeroCostCodeOptionGap gap, string requested) =>
        string.Equals(gap.Code.Trim(), requested, StringComparison.OrdinalIgnoreCase)
        || string.Equals(gap.OptionName, requested, StringComparison.OrdinalIgnoreCase);
}

// ── rename one option ─────────────────────────────────────────────────────────────────────

public sealed class RenameXeroCostCodeOptionAuthorisation
{
    public bool Allows(SignedInUser user, RenameXeroCostCodeOption command) =>
        XeroCostCodeOptionRoles.ManageCostCodeOptions.IncludesAny(user.Roles);
}

public sealed class RenameXeroCostCodeOptionValidation
{
    public ValidationOutcome Check(RenameXeroCostCodeOption command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.CurrentName)) errors.Add("currentName is required — the option's name exactly as Xero holds it.");
        if (string.IsNullOrWhiteSpace(command.NewName)) errors.Add("newName is required.");
        else if (command.NewName.Trim().Length > 100) errors.Add("newName must be 100 characters or fewer (Xero's limit).");
        if (errors.Count == 0 && string.Equals(command.CurrentName.Trim(), command.NewName.Trim(), StringComparison.Ordinal))
            errors.Add("newName is the same as currentName — nothing to rename.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class RenameXeroCostCodeOptionHandler : ICommandHandler<RenameXeroCostCodeOption, XeroCostCodeOptionRenameResult>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;
    private readonly AuditTrail audit;

    public RenameXeroCostCodeOptionHandler(JpmsContext context, IXeroClient xero, AuditTrail audit)
    { this.context = context; this.xero = xero; this.audit = audit; }

    public async Task<XeroCostCodeOptionRenameResult> HandleAsync(RenameXeroCostCodeOption command, CancellationToken cancellationToken)
    {
        var currentName = command.CurrentName.Trim();
        var newName = command.NewName.Trim();
        if (!xero.IsConfigured)
            return new XeroCostCodeOptionRenameResult(false, null, null, currentName, newName, Array.Empty<string>());

        var snapshot = await xero.GetTrackingCategoriesSnapshotAsync(force: true, cancellationToken);
        if (snapshot.Error is not null)
            return new XeroCostCodeOptionRenameResult(true, snapshot.Error, null, currentName, newName, Array.Empty<string>());
        var category = snapshot.Categories.FirstOrDefault(c => c.IsCostCodeCategory);
        if (category is null)
            return new XeroCostCodeOptionRenameResult(true,
                "Xero has no tracking category matching the configured Cost Code name.", null, currentName, newName, Array.Empty<string>());

        var option = category.Options.FirstOrDefault(o => string.Equals(o.Name.Trim(), currentName, StringComparison.OrdinalIgnoreCase));
        if (option is null)
            return new XeroCostCodeOptionRenameResult(true,
                $"Xero's \"{category.Name}\" category has no option named \"{currentName}\" — names must match exactly as Xero holds them.",
                null, currentName, newName, Array.Empty<string>());
        if (category.Options.Any(o => !ReferenceEquals(o, option) && string.Equals(o.Name.Trim(), newName, StringComparison.OrdinalIgnoreCase)))
            return new XeroCostCodeOptionRenameResult(true,
                $"Xero's \"{category.Name}\" category already has an option named \"{newName}\" — Xero refuses duplicate names.",
                null, currentName, newName, Array.Empty<string>());

        string optionId;
        try
        {
            optionId = await xero.RenameCostCodeOptionAsync(option.TrackingOptionId, newName, cancellationToken);
        }
        catch (XeroCallFailedException failure)
        {
            return new XeroCostCodeOptionRenameResult(true, failure.Message, null, currentName, newName, Array.Empty<string>());
        }

        // Consequences, stated: the history rewrite, and any portal code left coding under the
        // old name — the lazy create would quietly resurrect it the next time a bill needs it.
        var warnings = new List<string>
        {
            $"Xero applies renames to history: every bill line ever tracked as \"{option.Name}\" now reads \"{newName}\" in Xero's reports."
        };
        var codes = await XeroCostCodeOptionNames.ActiveCodesAsync(context, cancellationToken);
        var stillOnOldName = codes
            .Where(code => string.Equals(code.OptionName, currentName, StringComparison.OrdinalIgnoreCase))
            .Select(code => code.Code)
            .ToList();
        if (stillOnOldName.Count > 0)
            warnings.Add(
                $"Portal code(s) {string.Join(", ", stillOnOldName)} still code under \"{currentName}\" — revise the code in the "
                + "cost-code master or point its Xero mapping at the new name, otherwise the next bill against it recreates "
                + $"\"{currentName}\" in Xero.");

        await audit.WriteAsync(
            AuditEventType.XeroCostCodeOptionRenamed,
            $"Renamed \"{category.Name}\" tracking option \"{option.Name}\" → \"{newName}\" in Xero (history rewritten)"
            + (stillOnOldName.Count > 0 ? $"; portal code(s) still on the old name: {string.Join(", ", stillOnOldName)}" : ""),
            cancellationToken: cancellationToken);

        return new XeroCostCodeOptionRenameResult(true, null, optionId, option.Name, newName, warnings);
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

public sealed class CreateXeroCostCodeOptionsEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly CreateXeroCostCodeOptionsAuthorisation authorisation;
    private readonly CreateXeroCostCodeOptionsValidation validation;
    private readonly ICommandHandler<CreateXeroCostCodeOptions, XeroCostCodeOptionsCreateResult> handler;

    public CreateXeroCostCodeOptionsEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        CreateXeroCostCodeOptionsAuthorisation authorisation, CreateXeroCostCodeOptionsValidation validation,
        ICommandHandler<CreateXeroCostCodeOptions, XeroCostCodeOptionsCreateResult> handler)
    { this.users = users; this.auditActor = auditActor; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(CreateXeroCostCodeOptions))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/tracking-options/cost-codes/create")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email; // the trail records who pushed the codes to Xero

        var command = await request.ReadFromJsonAsync<CreateXeroCostCodeOptions>() ?? new CreateXeroCostCodeOptions();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}

public sealed class RenameXeroCostCodeOptionEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly RenameXeroCostCodeOptionAuthorisation authorisation;
    private readonly RenameXeroCostCodeOptionValidation validation;
    private readonly ICommandHandler<RenameXeroCostCodeOption, XeroCostCodeOptionRenameResult> handler;

    public RenameXeroCostCodeOptionEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        RenameXeroCostCodeOptionAuthorisation authorisation, RenameXeroCostCodeOptionValidation validation,
        ICommandHandler<RenameXeroCostCodeOption, XeroCostCodeOptionRenameResult> handler)
    { this.users = users; this.auditActor = auditActor; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(RenameXeroCostCodeOption))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "xero/tracking-options/cost-codes/rename")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        auditActor.Email = signedInUser.Email;

        var command = await request.ReadFromJsonAsync<RenameXeroCostCodeOption>();
        if (command is null) return new BadRequestResult();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(StatusCodes.Status403Forbidden);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);

        return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
    }
}
