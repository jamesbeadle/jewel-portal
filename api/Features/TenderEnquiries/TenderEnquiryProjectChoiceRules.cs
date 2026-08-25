using Jewel.JPMS.Contracts.TenderEnquiries;

namespace Jewel.JPMS.Api.Features.TenderEnquiries;

/// <summary>The either/or on where an enquiry lands — an existing project OR a new one — checked
/// the same way by both logging routes.</summary>
internal static class TenderEnquiryProjectChoiceRules
{
    public static List<string> Problems(string? projectId, TenderEnquiryProjectDraft? newProject)
    {
        var problems = new List<string>();
        var hasProject = !string.IsNullOrWhiteSpace(projectId);
        var hasDraft = newProject is not null;
        if (!hasProject && !hasDraft) problems.Add("Choose the project the enquiry belongs to, or describe the new one.");
        if (hasProject && hasDraft) problems.Add("An enquiry goes on an existing project OR a new one — not both.");
        if (hasDraft && string.IsNullOrWhiteSpace(newProject!.Name)) problems.Add("The new project needs a name.");
        if (hasDraft && !Enum.IsDefined(newProject!.Organisation)) problems.Add("Choose which Jewel entity the project belongs to.");
        return problems;
    }
}
