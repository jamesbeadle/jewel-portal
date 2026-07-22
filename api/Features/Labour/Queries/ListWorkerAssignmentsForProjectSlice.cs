using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour.Queries;

public sealed class ListWorkerAssignmentsForProjectEndpoint
{
    private readonly SignedInUserResolver users;
    private readonly IQueryHandler<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>> handler;
    public ListWorkerAssignmentsForProjectEndpoint(SignedInUserResolver users, IQueryHandler<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>> handler)
    { this.users = users; this.handler = handler; }

    [Function(nameof(ListWorkerAssignmentsForProject))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "projects/{projectId}/labour/assignments")] HttpRequest request, string projectId)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!JpmsRoleSets.AllInternal.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        return new OkObjectResult(await handler.HandleAsync(new ListWorkerAssignmentsForProject(projectId), request.HttpContext.RequestAborted));
    }
}

public sealed class ListWorkerAssignmentsForProjectHandler : IQueryHandler<ListWorkerAssignmentsForProject, IReadOnlyList<ProjectWorkerAssignment>>
{
    private readonly JpmsContext context;
    public ListWorkerAssignmentsForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ProjectWorkerAssignment>> HandleAsync(ListWorkerAssignmentsForProject query, CancellationToken cancellationToken)
    {
        var assignments = await context.ProjectWorkerAssignments
            .Where(assignment => assignment.ProjectId == query.ProjectId)
            .Join(context.Workers, assignment => assignment.WorkerId, worker => worker.WorkerId,
                (assignment, worker) => new { assignment, worker.Name })
            .OrderBy(row => row.Name)
            .ToListAsync(cancellationToken);
        return assignments.Select(row => row.assignment.ToModel(row.Name)).ToList();
    }
}
