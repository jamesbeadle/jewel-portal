using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

// Worker ↔ directory linking (2026-08-31, the accountant's month-end doc): the settlement gate
// (docs, item A/B). One id-keyed command the portal's own surfaces post
// (SetWorkerSettlementIdentity — the allocation page's inline fix and the Workers page's
// matching card), by-name wrappers for the connector, and the reconcile sweep that backfills the
// links the Xero import used to silently not create. Every link written lands on the audit trail
// (WorkerLinkedToDirectory): a counterparty decides where real money reconciles.

// ---- SetWorkerSettlementIdentity (id-keyed; the portal UI's command) --------------------------

public sealed class SetWorkerSettlementIdentityAuthorisation
{
    // The link is registry data — same gate as every worker write.
    public bool Allows(SignedInUser user, SetWorkerSettlementIdentity command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class SetWorkerSettlementIdentityValidation
{
    public ValidationOutcome Check(SetWorkerSettlementIdentity command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerId))
            errors.Add("workerId is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SetWorkerSettlementIdentityEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly SetWorkerSettlementIdentityAuthorisation authorisation;
    private readonly SetWorkerSettlementIdentityValidation validation;
    private readonly ICommandHandler<SetWorkerSettlementIdentity, Worker> handler;
    public SetWorkerSettlementIdentityEndpoint(SignedInUserResolver users,
        SetWorkerSettlementIdentityAuthorisation authorisation,
        SetWorkerSettlementIdentityValidation validation,
        ICommandHandler<SetWorkerSettlementIdentity, Worker> handler)
    { this.users = users; this.authorisation = authorisation; this.validation = validation; this.handler = handler; }

    [Function(nameof(SetWorkerSettlementIdentity))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers/{workerId}/settlement-identity")] HttpRequest request,
        string workerId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var body = await request.ReadFromJsonAsync<SetWorkerSettlementIdentity>();
        if (body is null) return new BadRequestResult();
        var command = body with { WorkerId = workerId };
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var outcome = validation.Check(command);
        if (outcome.HasFailed) return new BadRequestObjectResult(outcome.Errors);
        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException guard)
        {
            return new BadRequestObjectResult(new[] { guard.Message });
        }
    }
}

public sealed class SetWorkerSettlementIdentityHandler : ICommandHandler<SetWorkerSettlementIdentity, Worker>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public SetWorkerSettlementIdentityHandler(JpmsContext context, AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public async Task<Worker> HandleAsync(SetWorkerSettlementIdentity command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException($"Worker {command.WorkerId} not found.");

        string? subcontractorId = string.IsNullOrWhiteSpace(command.SubcontractorId) ? null : command.SubcontractorId;
        string companyName = "";
        if (subcontractorId is not null)
        {
            var company = await context.Subcontractors.AsNoTracking()
                .FirstOrDefaultAsync(sub => sub.SubcontractorId == subcontractorId, cancellationToken)
                ?? throw new InvalidOperationException("That directory record does not exist.");
            if (company.IsProspect)
                throw new InvalidOperationException(
                    $"{company.CompanyName} is a tender-only prospect — promote it into the Directory before workers settle through it.");
            companyName = company.CompanyName;
        }

        var before = worker.SubcontractorId is null && !worker.IsSoleTrader ? "no settlement identity"
            : worker.SubcontractorId is not null ? $"company {worker.SubcontractorId}"
            : "sole trader";
        worker.SubcontractorId = subcontractorId;
        worker.IsSoleTrader = command.IsSoleTrader;
        await context.SaveChangesAsync(cancellationToken);

        var after = subcontractorId is not null ? $"linked to {companyName}"
            : command.IsSoleTrader ? "flagged sole trader (settles under their own name)"
            : "settlement identity cleared";
        await audit.WriteAsync(
            AuditEventType.WorkerLinkedToDirectory,
            $"{worker.Name}: {before} → {after}.",
            cancellationToken: cancellationToken);

        return worker.ToModel();
    }
}

// ---- Shared by-name resolution ----------------------------------------------------------------

internal static class WorkerLinkResolution
{
    /// <summary>The directory company the user named — non-prospect records only, matched with
    /// the shared labour name rule; unambiguous or refused with the candidates.</summary>
    public static async Task<(string SubcontractorId, string CompanyName)> ResolveCompanyAsync(
        JpmsContext context, string companyName, CancellationToken cancellationToken)
    {
        var companies = await context.Subcontractors.AsNoTracking()
            .Where(sub => !sub.IsProspect)
            .Select(sub => new { sub.SubcontractorId, sub.CompanyName })
            .ToListAsync(cancellationToken);
        var matches = companies
            .Where(sub => WorkerDirectoryMatcher.Matches(sub.CompanyName, companyName))
            .ToList();
        if (matches.Count == 1) return (matches[0].SubcontractorId, matches[0].CompanyName);
        if (matches.Count > 1)
            throw new InvalidOperationException(
                $"\"{companyName}\" matches more than one directory record: "
                + string.Join(", ", matches.Select(sub => sub.CompanyName))
                + ". Use the company name as the directory spells it.");
        throw new InvalidOperationException(
            $"No directory record matches \"{companyName}\" — list_subcontractors shows the directory. "
            + "If the company only exists in Xero, import it first (import_xero_supplier), which now "
            + "auto-links matching workers.");
    }
}

// ---- link_worker_to_company -------------------------------------------------------------------

public sealed class LinkWorkerToCompanyByNameAuthorisation
{
    public bool Allows(SignedInUser user, LinkWorkerToCompanyByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class LinkWorkerToCompanyByNameValidation
{
    public ValidationOutcome Check(LinkWorkerToCompanyByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        if (string.IsNullOrWhiteSpace(command.CompanyName)) errors.Add("Company name is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class LinkWorkerToCompanyByNameHandler : ICommandHandler<LinkWorkerToCompanyByName, Worker>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<SetWorkerSettlementIdentity, Worker> identity;
    public LinkWorkerToCompanyByNameHandler(JpmsContext context, ICommandHandler<SetWorkerSettlementIdentity, Worker> identity)
    { this.context = context; this.identity = identity; }

    public async Task<Worker> HandleAsync(LinkWorkerToCompanyByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "linking them to the directory");
        var (subcontractorId, _) = await WorkerLinkResolution.ResolveCompanyAsync(context, command.CompanyName, cancellationToken);
        // The company link wins over any sole-trader flag by rule; clearing the flag here keeps
        // the record honest rather than leaving a dormant flag behind the link.
        return await identity.HandleAsync(
            new SetWorkerSettlementIdentity(worker.WorkerId, subcontractorId, IsSoleTrader: false), cancellationToken);
    }
}

// ---- set_worker_sole_trader -------------------------------------------------------------------

public sealed class SetWorkerSoleTraderByNameAuthorisation
{
    public bool Allows(SignedInUser user, SetWorkerSoleTraderByName command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class SetWorkerSoleTraderByNameValidation
{
    public ValidationOutcome Check(SetWorkerSoleTraderByName command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.WorkerName)) errors.Add("Worker name is required.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SetWorkerSoleTraderByNameHandler : ICommandHandler<SetWorkerSoleTraderByName, Worker>
{
    private readonly JpmsContext context;
    private readonly ICommandHandler<SetWorkerSettlementIdentity, Worker> identity;
    public SetWorkerSoleTraderByNameHandler(JpmsContext context, ICommandHandler<SetWorkerSettlementIdentity, Worker> identity)
    { this.context = context; this.identity = identity; }

    public async Task<Worker> HandleAsync(SetWorkerSoleTraderByName command, CancellationToken cancellationToken)
    {
        var workers = await context.Workers.AsNoTracking().ToListAsync(cancellationToken);
        var worker = WorkerNameResolver.Resolve(workers, command.WorkerName, "flagging them a sole trader");
        if (worker.SubcontractorId is not null && command.IsSoleTrader)
            throw new InvalidOperationException(
                $"{worker.Name} is linked to a subcontractor company, which always wins over the "
                + "sole-trader flag — clear the link first if they really bill under their own name.");
        return await identity.HandleAsync(
            new SetWorkerSettlementIdentity(worker.WorkerId, worker.SubcontractorId, command.IsSoleTrader),
            cancellationToken);
    }
}

// ---- ReconcileWorkerDirectoryLinks (the backfill sweep) ---------------------------------------

public sealed class ReconcileWorkerDirectoryLinksAuthorisation
{
    public bool Allows(SignedInUser user, ReconcileWorkerDirectoryLinks command) =>
        LabourRoleSets.ManageWorkers.IncludesAny(user.Roles);
}

public sealed class ReconcileWorkerDirectoryLinksValidation
{
    public ValidationOutcome Check(ReconcileWorkerDirectoryLinks command) => ValidationOutcome.Passed;
}

public sealed class ReconcileWorkerDirectoryLinksEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ReconcileWorkerDirectoryLinksAuthorisation authorisation;
    private readonly ReconcileWorkerDirectoryLinksHandler handler;
    public ReconcileWorkerDirectoryLinksEndpoint(SignedInUserResolver users,
        ReconcileWorkerDirectoryLinksAuthorisation authorisation, ReconcileWorkerDirectoryLinksHandler handler)
    { this.users = users; this.authorisation = authorisation; this.handler = handler; }

    [Function(nameof(ReconcileWorkerDirectoryLinks))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "labour/workers/reconcile-links")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<ReconcileWorkerDirectoryLinks>();
        if (command is null) return new BadRequestResult();
        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(
            command with { LinkedByEmail = signedInUser.Email }, request.HttpContext.RequestAborted));
    }
}

public sealed class ReconcileWorkerDirectoryLinksHandler : ICommandHandler<ReconcileWorkerDirectoryLinks, WorkerDirectoryLinkReport>
{
    private readonly JpmsContext context;
    private readonly AuditTrail audit;
    public ReconcileWorkerDirectoryLinksHandler(JpmsContext context, AuditTrail audit)
    { this.context = context; this.audit = audit; }

    public async Task<WorkerDirectoryLinkReport> HandleAsync(ReconcileWorkerDirectoryLinks command, CancellationToken cancellationToken)
    {
        // Only workers with no settlement identity at all: linked and sole-trader workers are
        // already reconcilable, and the sweep never second-guesses a decision a human has made.
        var workers = await context.Workers
            .Where(worker => worker.IsActive && worker.SubcontractorId == null && !worker.IsSoleTrader)
            .OrderBy(worker => worker.Name)
            .ToListAsync(cancellationToken);
        var companies = await context.Subcontractors.AsNoTracking()
            .Where(sub => !sub.IsProspect)
            .Select(sub => new { sub.SubcontractorId, sub.CompanyName })
            .ToListAsync(cancellationToken);

        var outcomes = new List<WorkerDirectoryLinkOutcome>();
        var linked = new List<string>();
        foreach (var worker in workers)
        {
            var matches = companies
                .Where(sub => WorkerDirectoryMatcher.Matches(sub.CompanyName, worker.Name))
                .Select(sub => new WorkerDirectoryLinkCandidate(sub.SubcontractorId, sub.CompanyName))
                .ToList();

            if (matches.Count == 1)
            {
                if (command.Apply)
                {
                    worker.SubcontractorId = matches[0].SubcontractorId;
                    linked.Add($"{worker.Name} → {matches[0].CompanyName}");
                }
                outcomes.Add(new WorkerDirectoryLinkOutcome(worker.WorkerId, worker.Name,
                    command.Apply ? "linked" : "would link", matches[0], Array.Empty<WorkerDirectoryLinkCandidate>()));
            }
            else if (matches.Count > 1)
            {
                outcomes.Add(new WorkerDirectoryLinkOutcome(worker.WorkerId, worker.Name,
                    "ambiguous", null, matches));
            }
            else
            {
                outcomes.Add(new WorkerDirectoryLinkOutcome(worker.WorkerId, worker.Name,
                    "unmatched", null, Array.Empty<WorkerDirectoryLinkCandidate>()));
            }
        }

        if (command.Apply && linked.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            foreach (var line in linked)
                await audit.WriteAsync(
                    AuditEventType.WorkerLinkedToDirectory,
                    $"Reconcile sweep: {line}.",
                    actorEmail: command.LinkedByEmail,
                    cancellationToken: cancellationToken);
        }

        return new WorkerDirectoryLinkReport(command.Apply, outcomes);
    }
}
