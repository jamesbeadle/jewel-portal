using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Migration sweep from legacy flat request tags ("JPMS/RFI-012") to project-qualified ones
/// ("JPMS/JBB-2026-001-RFI-012"). Walks every request and moves its tagged mail with the same
/// verified <see cref="IMailboxGraphClient.RetagAsync"/> used when a reference is edited. Per-request
/// failures are logged and counted, never fatal — re-running the sweep picks up whatever a transient
/// Graph failure left behind, and requests whose old tag has no mail are free no-ops.
/// </summary>
public sealed class RetagRequestWorkflowTagsHandler : ICommandHandler<RetagRequestWorkflowTags, RequestRetagSummary>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly ILogger<RetagRequestWorkflowTagsHandler> logger;

    public RetagRequestWorkflowTagsHandler(JpmsContext context, IMailboxGraphClient graph, ILogger<RetagRequestWorkflowTagsHandler> logger)
    {
        this.context = context;
        this.graph = graph;
        this.logger = logger;
    }

    public async Task<RequestRetagSummary> HandleAsync(RetagRequestWorkflowTags command, CancellationToken cancellationToken)
    {
        var projectRefs = await context.Projects.AsNoTracking()
            .ToDictionaryAsync(p => p.ProjectId, p => p.Reference, cancellationToken);

        var requests = await context.Requests.AsNoTracking().ToListAsync(cancellationToken);

        var processed = 0;
        var moved = 0;
        var failures = 0;

        foreach (var request in requests)
        {
            var legacyTag = TriageCategories.ForRecord(request.TagReference);
            var qualifiedTag = TriageCategories.ForRecord(RequestTags.Stem(
                projectRefs.GetValueOrDefault(request.ProjectId), request.ProjectId, request.TagReference));
            if (string.Equals(legacyTag, qualifiedTag, StringComparison.OrdinalIgnoreCase)) continue;

            processed++;
            try
            {
                moved += await graph.RetagAsync(legacyTag, qualifiedTag, cancellationToken);
            }
            catch (Exception ex)
            {
                failures++;
                logger.LogWarning(ex, "Retag sweep: {OldTag} -> {NewTag} failed; run the sweep again to catch it up.",
                    legacyTag, qualifiedTag);
            }
        }

        logger.LogInformation("Retag sweep complete: {Processed} request(s) processed, {Moved} email(s) moved, {Failures} failure(s).",
            processed, moved, failures);
        return new RequestRetagSummary(processed, moved, failures);
    }
}
