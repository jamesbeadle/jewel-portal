using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Kpi.Commands;
using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Api.Features.Kpi;

// The KPI register's HTTP surface — every route refuses anyone but an administrator BEFORE any
// handler runs, with a 403 that says so plainly (the register's existence is not a secret; its
// contents are). One class, four functions: the slice is small and gated identically throughout.
public sealed class KpiEndpoints
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly IQueryHandler<ListKpiEmails, IReadOnlyList<KpiEmail>> list;
    private readonly IQueryHandler<ListKpiPeople, IReadOnlyList<KpiPerson>> listPeople;
    private readonly AddKpiPersonAuthorisation addPersonAuthorisation;
    private readonly AddKpiPersonValidation addPersonValidation;
    private readonly ICommandHandler<AddKpiPerson, KpiPerson> addPerson;
    private readonly MarkEmailAsKpiAuthorisation markAuthorisation;
    private readonly MarkEmailAsKpiValidation markValidation;
    private readonly ICommandHandler<MarkEmailAsKpi, KpiEmail> mark;
    private readonly UpdateKpiEmailAuthorisation updateAuthorisation;
    private readonly UpdateKpiEmailValidation updateValidation;
    private readonly ICommandHandler<UpdateKpiEmail, KpiEmail> update;
    private readonly RemoveKpiEmailAuthorisation removeAuthorisation;
    private readonly ICommandHandler<RemoveKpiEmail, Acknowledgement> remove;

    public KpiEndpoints(
        SignedInUserResolver users,
        AuditActor auditActor,
        IQueryHandler<ListKpiEmails, IReadOnlyList<KpiEmail>> list,
        IQueryHandler<ListKpiPeople, IReadOnlyList<KpiPerson>> listPeople,
        AddKpiPersonAuthorisation addPersonAuthorisation,
        AddKpiPersonValidation addPersonValidation,
        ICommandHandler<AddKpiPerson, KpiPerson> addPerson,
        MarkEmailAsKpiAuthorisation markAuthorisation,
        MarkEmailAsKpiValidation markValidation,
        ICommandHandler<MarkEmailAsKpi, KpiEmail> mark,
        UpdateKpiEmailAuthorisation updateAuthorisation,
        UpdateKpiEmailValidation updateValidation,
        ICommandHandler<UpdateKpiEmail, KpiEmail> update,
        RemoveKpiEmailAuthorisation removeAuthorisation,
        ICommandHandler<RemoveKpiEmail, Acknowledgement> remove)
    {
        this.users = users; this.auditActor = auditActor; this.list = list;
        this.listPeople = listPeople; this.addPersonAuthorisation = addPersonAuthorisation;
        this.addPersonValidation = addPersonValidation; this.addPerson = addPerson;
        this.markAuthorisation = markAuthorisation; this.markValidation = markValidation; this.mark = mark;
        this.updateAuthorisation = updateAuthorisation; this.updateValidation = updateValidation; this.update = update;
        this.removeAuthorisation = removeAuthorisation; this.remove = remove;
    }

    private static ObjectResult AdministratorsOnly() =>
        new("The KPI register is for administrators only.") { StatusCode = StatusCodes.Status403Forbidden };

    [Function(nameof(ListKpiEmails))]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "kpi/emails")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!KpiRoles.IsAdministrator(signedInUser)) return AdministratorsOnly();
        var personId = request.Query["person"].ToString();
        var query = new ListKpiEmails(string.IsNullOrWhiteSpace(personId) ? null : personId);
        return new OkObjectResult(await list.HandleAsync(query, request.HttpContext.RequestAborted));
    }

    [Function(nameof(ListKpiPeople))]
    public async Task<IActionResult> People(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "kpi/people")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!KpiRoles.IsAdministrator(signedInUser)) return AdministratorsOnly();
        return new OkObjectResult(await listPeople.HandleAsync(new ListKpiPeople(), request.HttpContext.RequestAborted));
    }

    [Function(nameof(AddKpiPerson))]
    public async Task<IActionResult> AddPerson(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "kpi/people")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<AddKpiPerson>();
        if (command is null) return new BadRequestObjectResult("name is required.");
        auditActor.Email = signedInUser.Email;
        if (!addPersonAuthorisation.Allows(signedInUser, command)) return AdministratorsOnly();
        var validationOutcome = addPersonValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await addPerson.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(MarkEmailAsKpi))]
    public async Task<IActionResult> Mark(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "kpi/emails")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var posted = await request.ReadFromJsonAsync<MarkEmailAsKpi>();
        if (posted is null) return new BadRequestObjectResult("messageId and a person (personId / personEmail / personName) are required.");
        // The marker is the signed-in user, stamped here — never taken from the body.
        var command = posted with { MarkedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;
        if (!markAuthorisation.Allows(signedInUser, command)) return AdministratorsOnly();
        var validationOutcome = markValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await mark.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            // Business refusals (unknown user, unreadable email) read back rather than a 500.
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(UpdateKpiEmail))]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "kpi/emails/{kpiEmailId}")] HttpRequest request, string kpiEmailId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = await request.ReadFromJsonAsync<UpdateKpiEmail>();
        if (command is null) return new BadRequestResult();
        if (command.KpiEmailId != kpiEmailId) return new BadRequestObjectResult("Route kpiEmailId does not match body.");
        auditActor.Email = signedInUser.Email;
        if (!updateAuthorisation.Allows(signedInUser, command)) return AdministratorsOnly();
        var validationOutcome = updateValidation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);
        try
        {
            return new OkObjectResult(await update.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    [Function(nameof(RemoveKpiEmail))]
    public async Task<IActionResult> Remove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "kpi/emails/{kpiEmailId}")] HttpRequest request, string kpiEmailId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        var command = new RemoveKpiEmail(kpiEmailId);
        auditActor.Email = signedInUser.Email;
        if (!removeAuthorisation.Allows(signedInUser, command)) return AdministratorsOnly();
        try
        {
            return new OkObjectResult(await remove.HandleAsync(command, request.HttpContext.RequestAborted));
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
