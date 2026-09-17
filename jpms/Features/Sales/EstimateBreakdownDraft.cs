using System.Globalization;
using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Sales;

/// <summary>The priced breakdown as it is being typed — seeded from the record once per load and
/// after every save, never on a re-render, so a keystroke is never wiped by the read model waking up.</summary>
public sealed class EstimateBreakdownDraft
{
    public List<EstimateSectionDraft> Sections { get; } = new();

    public decimal GrandTotal => Sections.Sum(section => section.Total);

    public static EstimateBreakdownDraft From(LeadEstimate estimate)
    {
        var draft = new EstimateBreakdownDraft();
        foreach (var section in estimate.Sections)
            draft.Sections.Add(new EstimateSectionDraft
            {
                Name = section.Name,
                Provisional = section.Provisional,
                Lines = section.Lines.Select(line => new EstimateLineDraft
                {
                    CostCode = line.CostCode, Description = line.Description, Unit = line.Unit,
                    QuantityText = line.Quantity.ToString("0.####", CultureInfo.InvariantCulture),
                    UnitPriceText = line.UnitPrice.ToString("0.##", CultureInfo.InvariantCulture)
                }).ToList()
            });
        return draft;
    }

    public void AddSection() => Sections.Add(new EstimateSectionDraft { Lines = { new EstimateLineDraft() } });

    public void Remove(EstimateSectionDraft section) => Sections.Remove(section);

    public void Move(EstimateSectionDraft section, int delta)
    {
        var at = Sections.IndexOf(section);
        var to = at + delta;
        if (at < 0 || to < 0 || to >= Sections.Count) return;
        Sections.RemoveAt(at);
        Sections.Insert(to, section);
    }

    public List<EstimateBreakdownSection> ToPayload() =>
        Sections.Select(s => new EstimateBreakdownSection(
            s.Name.Trim(), s.Provisional,
            s.Lines.Where(l => !l.IsBlank)
                .Select(l => new EstimateBreakdownLine(l.CostCode.Trim(), l.Description.Trim(), l.Quantity, l.Unit.Trim(), l.UnitPrice))
                .ToList())).ToList();
}
