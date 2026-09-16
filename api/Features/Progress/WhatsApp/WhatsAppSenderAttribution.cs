using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>Whose messages a sender's are: this project's, another project's, or nobody has said.
/// Attribution is by sender against the sender lists held in Project settings — a name on this
/// project's list wins even if another project lists it too.</summary>
public enum WhatsAppSenderSite
{
    ThisProject = 0,
    OtherProject = 1,
    Unknown = 2
}

public sealed class WhatsAppSenderAttribution
{
    private readonly IReadOnlyList<string> thisProject;
    private readonly IReadOnlyList<string> otherProjects;

    public WhatsAppSenderAttribution(IReadOnlyList<string> thisProject, IReadOnlyList<string> otherProjects)
    {
        this.thisProject = thisProject;
        this.otherProjects = otherProjects;
    }

    public bool HasAnyoneListed => thisProject.Count > 0;

    public WhatsAppSenderSite SiteOf(string sender)
    {
        if (SiteNoteSenders.Includes(thisProject, sender)) return WhatsAppSenderSite.ThisProject;
        if (SiteNoteSenders.Includes(otherProjects, sender)) return WhatsAppSenderSite.OtherProject;
        return WhatsAppSenderSite.Unknown;
    }
}
