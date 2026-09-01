using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Api.Features.Requests;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.RecordLinks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.BuildingControl.Commands;

/// <summary>
/// Raises an inspection stage from the inspector's email — a booking confirmation, a visit
/// arrangement. Order of work: pre-flight the cross-pathway confirm (free to refuse before
/// anything exists); then the inspection row on the project's ACTIVE case; then the email tag
/// through the shared link path, so the tag matches the provider (the
/// CreateCalendarEventFromMessage shape). The case must already exist — an inspection is never
/// raised into thin air; the error tells the triager to set the case up on the tab first.
/// </summary>
public sealed class CreateBuildingControlInspectionFromMessageHandler
    : ICommandHandler<CreateBuildingControlInspectionFromMessage, BuildingControlInspection>
{
    private const string NewRecordLabel = "the new building control inspection";

    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly BuildingControlInspectionRegister register;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;

    public CreateBuildingControlInspectionFromMessageHandler(
        JpmsContext context, IMailboxGraphClient graph, BuildingControlInspectionRegister register,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link)
    {
        this.context = context;
        this.graph = graph;
        this.register = register;
        this.link = link;
    }

    public async Task<BuildingControlInspection> HandleAsync(
        CreateBuildingControlInspectionFromMessage command, CancellationToken cancellationToken)
    {
        var snapshot = await graph.GetSnapshotAsync(command.MessageId, command.InternetMessageId, cancellationToken)
            ?? throw new InvalidOperationException("The email could not be read from the mailbox.");
        CrossPathwayGuard.EnsureConfirmed(
            snapshot.Categories, TriageCategories.BucketFor(RecordType.BuildingControlInspection),
            command.AllowCrossPathway, NewRecordLabel);

        var cases = await context.BuildingControlCases.AsNoTracking()
            .Where(row => row.ProjectId == command.ProjectId)
            .ToListAsync(cancellationToken);
        var activeCase = cases.Where(BuildingControlRules.IsActive).OrderByDescending(row => row.Number).FirstOrDefault()
            ?? throw new InvalidOperationException(
                "This project has no building control case yet — set one up on its Building Control tab first, then raise the inspection.");

        var entity = await register.RaiseAsync(activeCase, command.Details, command.CreatedByEmail, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await link.HandleAsync(
            new LinkMessageToRecord(
                command.MessageId, RecordType.BuildingControlInspection, entity.BuildingControlInspectionId,
                command.InternetMessageId,
                AllowCrossPathway: command.AllowCrossPathway, Scope: command.Scope),
            cancellationToken);

        return entity.ToModel();
    }
}

/// <summary>Raising an inspection from an email is a triage act, so it carries the triage gate —
/// the same stance as CreateCalendarEventFromMessage.</summary>
public sealed class CreateBuildingControlInspectionFromMessageAuthorisation
{
    public bool Allows(SignedInUser user, CreateBuildingControlInspectionFromMessage command) =>
        TriageRoles.AllowedToTriage.IncludesAny(user.Roles);
}

public sealed class CreateBuildingControlInspectionFromMessageValidation
{
    public ValidationOutcome Check(CreateBuildingControlInspectionFromMessage command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.MessageId)) errors.Add("MessageId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        errors.AddRange(BuildingControlRules.InspectionProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class CreateBuildingControlInspectionFromMessageEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly AuditActor auditActor;
    private readonly CreateBuildingControlInspectionFromMessageAuthorisation authorisation;
    private readonly CreateBuildingControlInspectionFromMessageValidation validation;
    private readonly ICommandHandler<CreateBuildingControlInspectionFromMessage, BuildingControlInspection> handler;

    public CreateBuildingControlInspectionFromMessageEndpoint(
        SignedInUserResolver users, AuditActor auditActor,
        CreateBuildingControlInspectionFromMessageAuthorisation authorisation,
        CreateBuildingControlInspectionFromMessageValidation validation,
        ICommandHandler<CreateBuildingControlInspectionFromMessage, BuildingControlInspection> handler)
    {
        this.users = users;
        this.auditActor = auditActor;
        this.authorisation = authorisation;
        this.validation = validation;
        this.handler = handler;
    }

    [Function(nameof(CreateBuildingControlInspectionFromMessage))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/message/create-building-control-inspection")] HttpRequest request)
    {
        var cancellationToken = request.HttpContext.RequestAborted;
        var signedInUser = await users.ResolveAsync(request, cancellationToken);
        if (signedInUser is null) return new UnauthorizedResult();

        var posted = await request.ReadFromJsonAsync<CreateBuildingControlInspectionFromMessage>(cancellationToken);
        if (posted is null || string.IsNullOrWhiteSpace(posted.MessageId))
            return new BadRequestObjectResult("messageId is required.");

        // The creator is always the signed-in user — never trusted from the client body.
        var command = posted with { CreatedByEmail = signedInUser.Email };
        auditActor.Email = signedInUser.Email;

        if (!authorisation.Allows(signedInUser, command)) return new StatusCodeResult(403);
        var validationOutcome = validation.Check(command);
        if (validationOutcome.HasFailed) return new BadRequestObjectResult(validationOutcome.Errors);

        try
        {
            return new OkObjectResult(await handler.HandleAsync(command, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            // Guards (no case yet, cross-pathway confirm, unreadable email) are answers the
            // triager acts on — a bodiless 500 would hide them.
            return new BadRequestObjectResult(ex.Message);
        }
    }
}
