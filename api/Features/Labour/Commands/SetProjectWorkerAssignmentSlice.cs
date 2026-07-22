using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Commands;

public sealed class SetProjectWorkerAssignmentEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly ICommandHandler<SetProjectWorkerAssignment, ProjectWorkerAssignment> handler;
    public SetProjectWorkerAssignmentEndpoint(SignedInUserResolver users, ICommandHandler<SetProjectWorkerAssignment, ProjectWorkerAssignment> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(SetProjectWorkerAssignment))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "projects/{projectId}/labour/assignments")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!LabourRoleSets.ManageWorkers.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        var body = await request.ReadFromJsonAsync<SetProjectWorkerAssignment>();
        if (body is null || string.IsNullOrWhiteSpace(body.WorkerId)) return new BadRequestResult();
        return new OkObjectResult(await handler.HandleAsync(body with { ProjectId = projectId }, request.HttpContext.RequestAborted));
    }
}

public sealed class SetProjectWorkerAssignmentHandler : ICommandHandler<SetProjectWorkerAssignment, ProjectWorkerAssignment>
{
    private readonly JpmsContext context;
    public SetProjectWorkerAssignmentHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectWorkerAssignment> HandleAsync(SetProjectWorkerAssignment command, CancellationToken cancellationToken)
    {
        var worker = await context.Workers.FindAsync(new object[] { command.WorkerId }, cancellationToken)
            ?? throw new InvalidOperationException($"Worker {command.WorkerId} not found.");

        var assignment = await context.ProjectWorkerAssignments.FirstOrDefaultAsync(
            row => row.ProjectId == command.ProjectId && row.WorkerId == command.WorkerId, cancellationToken);

        if (assignment is null)
        {
            assignment = new ProjectWorkerAssignmentEntity
            {
                ProjectWorkerAssignmentId = LabourIdentifierFactory.NextProjectWorkerAssignmentId(),
                ProjectId = command.ProjectId,
                WorkerId = command.WorkerId,
            };
            context.ProjectWorkerAssignments.Add(assignment);
        }
        assignment.IsActive = command.IsActive;
        await context.SaveChangesAsync(cancellationToken);
        return assignment.ToModel(worker.Name);
    }
}
