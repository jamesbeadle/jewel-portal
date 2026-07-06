using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Site;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class AddProgrammeTaskLinkHandler : ICommandHandler<AddProgrammeTaskLink, ProgrammeTaskLink>
{
    private readonly JpmsContext context;
    public AddProgrammeTaskLinkHandler(JpmsContext context) { this.context = context; }

    public async Task<ProgrammeTaskLink> HandleAsync(AddProgrammeTaskLink command, CancellationToken cancellationToken)
    {
        // Both ends must be real tasks on this project — links are meaningless across projects.
        var taskIds = await context.ProgrammeTasks
            .Where(t => t.ProjectId == command.ProjectId
                        && (t.ProgrammeTaskId == command.PredecessorTaskId || t.ProgrammeTaskId == command.SuccessorTaskId))
            .Select(t => t.ProgrammeTaskId)
            .ToListAsync(cancellationToken);
        if (!taskIds.Contains(command.PredecessorTaskId) || !taskIds.Contains(command.SuccessorTaskId))
            throw new InvalidOperationException("Both tasks must exist on the project before they can be linked.");

        var existing = await context.ProgrammeTaskLinks
            .Where(l => l.ProjectId == command.ProjectId)
            .ToListAsync(cancellationToken);

        if (existing.Any(l => l.PredecessorTaskId == command.PredecessorTaskId && l.SuccessorTaskId == command.SuccessorTaskId))
            throw new InvalidOperationException("That dependency already exists.");

        // Reject cycles: if the predecessor is already reachable from the successor, adding this
        // link would make the programme impossible to sequence.
        if (Reaches(existing, from: command.SuccessorTaskId, to: command.PredecessorTaskId))
            throw new InvalidOperationException("That dependency would create a cycle in the programme.");

        var entity = new ProgrammeTaskLinkEntity
        {
            ProgrammeTaskLinkId = SiteIdentifierFactory.NextProgrammeTaskLinkId(),
            ProjectId = command.ProjectId,
            PredecessorTaskId = command.PredecessorTaskId,
            SuccessorTaskId = command.SuccessorTaskId,
            LagDays = command.LagDays
        };
        context.ProgrammeTaskLinks.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    // Depth-first reachability over the existing links: is 'to' downstream of 'from'?
    private static bool Reaches(IReadOnlyList<ProgrammeTaskLinkEntity> links, string from, string to)
    {
        if (from == to) return true;
        var successorsByPredecessor = links
            .GroupBy(l => l.PredecessorTaskId)
            .ToDictionary(g => g.Key, g => g.Select(l => l.SuccessorTaskId).ToList());

        var seen = new HashSet<string>();
        var stack = new Stack<string>();
        stack.Push(from);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == to) return true;
            if (!seen.Add(current)) continue;
            if (!successorsByPredecessor.TryGetValue(current, out var successors)) continue;
            foreach (var next in successors) stack.Push(next);
        }
        return false;
    }
}
