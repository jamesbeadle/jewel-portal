using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemoryBoqStore : IBoqStore
{
    private readonly List<BoqLineItem> lines = new()
    {
        new("BL-001", "PRJ-001", "Strip foundations to design", "m³",  60m, 185m, "GW-100", Discipline.Structural),
        new("BL-002", "PRJ-001", "Reinforced ground floor slab", "m²", 180m,  95m, "GW-110", Discipline.Structural),
        new("BL-003", "PRJ-001", "External facing brickwork",    "m²", 320m, 142m, "EX-200", Discipline.External),
        new("BL-004", "PRJ-001", "Natural slate roof covering",  "m²", 240m, 220m, "RF-300", Discipline.Architectural),
        new("BL-005", "PRJ-001", "1st fix electrical sockets",   "nr",  85m,  58m, "EL-400", Discipline.Electrical),
        new("BL-006", "PRJ-001", "Bespoke oak staircase",        "nr",   1m,8400m, "JN-500", Discipline.Joinery)
    };

    public event Action? OnChange;

    public IReadOnlyList<BoqLineItem> LinesFor(string projectId) =>
        lines.Where(line =>
            string.Equals(line.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
             .ToList()
             .AsReadOnly();

    public BoqLineItem Upsert(BoqLineItem line)
    {
        var existing = lines.FirstOrDefault(item => item.BoqLineItemId == line.BoqLineItemId);
        if (existing is not null) lines.Remove(existing);
        lines.Add(line);
        OnChange?.Invoke();
        return line;
    }

    public bool Remove(string boqLineItemId)
    {
        var existing = lines.FirstOrDefault(item => item.BoqLineItemId == boqLineItemId);
        if (existing is null) return false;
        lines.Remove(existing);
        OnChange?.Invoke();
        return true;
    }

    public decimal TotalFor(string projectId) =>
        LinesFor(projectId).Sum(line => line.LineTotal);
}
