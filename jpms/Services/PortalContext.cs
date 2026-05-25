namespace Jewel.JPMS.Services;

public sealed class PortalContext
{
    public string? ActingSubcontractorId { get; private set; }
    public string? ActingArchitectEmail { get; private set; }
    public string? ActingClientProjectId { get; private set; }

    public event Action? OnChange;

    public void ActAsSubcontractor(string subcontractorId)
    {
        ActingSubcontractorId = subcontractorId;
        OnChange?.Invoke();
    }

    public void ActAsArchitect(string architectEmail)
    {
        ActingArchitectEmail = architectEmail;
        OnChange?.Invoke();
    }

    public void ActAsClientOn(string projectId)
    {
        ActingClientProjectId = projectId;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        ActingSubcontractorId = null;
        ActingArchitectEmail = null;
        ActingClientProjectId = null;
        OnChange?.Invoke();
    }
}
