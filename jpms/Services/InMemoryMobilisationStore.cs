using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryMobilisationStore : IMobilisationStore
{
    private const string NigelEmail = "Nigel.Reilly@jewelenterprises.co.uk";

    private readonly List<MobilisationItem> items = new()
    {
        new("MB-001", "PRJ-001", "Site set-up and welfare in place",           NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-50)),
        new("MB-002", "PRJ-001", "Hoarding and signage installed",              NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-49)),
        new("MB-003", "PRJ-001", "F10 notification filed with HSE",             NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-48)),
        new("MB-004", "PRJ-001", "Site Manager inducted",                       NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-48)),
        new("MB-005", "PRJ-001", "First fix subcontractor RAMS approved",       NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-45)),

        new("MB-006", "PRJ-002", "Site set-up and welfare in place",            NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-20)),
        new("MB-007", "PRJ-002", "Hoarding and signage installed",              NigelEmail, true,  DateTimeOffset.UtcNow.AddDays(-19)),
        new("MB-008", "PRJ-002", "F10 notification filed with HSE",             NigelEmail, false, null),
        new("MB-009", "PRJ-002", "Site Manager inducted",                       NigelEmail, false, null),
        new("MB-010", "PRJ-002", "First fix subcontractor RAMS approved",       NigelEmail, false, null)
    };

    public event Action? OnChange;

    public MobilisationChecklist For(string projectId)
    {
        var itemsForProject = items
            .Where(item => string.Equals(item.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
        return new MobilisationChecklist(projectId, itemsForProject);
    }

    public void ToggleItem(string mobilisationItemId)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].MobilisationItemId != mobilisationItemId) continue;
            var nextComplete = !items[i].IsComplete;
            items[i] = items[i] with
            {
                IsComplete = nextComplete,
                CompletedAt = nextComplete ? DateTimeOffset.UtcNow : null
            };
            OnChange?.Invoke();
            return;
        }
    }
}
