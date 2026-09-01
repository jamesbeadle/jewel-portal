using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Commands;

/// <summary>Which project an enquiry lands on: the existing one named, or the Lead-stage shell
/// created for it. One rule for the email route and the by-hand route.</summary>
public sealed class TenderEnquiryProjectResolver
{
    private readonly JpmsContext context;
    private readonly TenderEnquiryProjectCreator projectCreator;

    public TenderEnquiryProjectResolver(JpmsContext context, TenderEnquiryProjectCreator projectCreator)
    {
        this.context = context;
        this.projectCreator = projectCreator;
    }

    public async Task<(string ProjectId, bool Created)> ResolveAsync(
        string? projectId, TenderEnquiryProjectDraft? newProject, TenderEnquiryDetails details,
        string projectManagerEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            var exists = await context.Projects.AnyAsync(row => row.ProjectId == projectId, cancellationToken);
            if (!exists) throw new InvalidOperationException($"Project '{projectId}' not found.");
            return (projectId, false);
        }
        var draft = newProject
            ?? throw new InvalidOperationException("Either an existing project or the details of a new one are required.");
        var project = await projectCreator.CreateAsync(draft, details, projectManagerEmail, cancellationToken);
        return (project.ProjectId, true);
    }
}
