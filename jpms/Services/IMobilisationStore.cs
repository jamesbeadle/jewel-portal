using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IMobilisationStore
{
    MobilisationChecklist For(string projectId);
    void ToggleItem(string mobilisationItemId);
    event Action? OnChange;
}
