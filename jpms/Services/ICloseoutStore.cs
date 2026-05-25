using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ICloseoutStore
{
    IReadOnlyList<Defect> DefectsFor(string projectId);
    Defect SaveDefect(Defect defect);

    SettlementRecord? SettlementFor(string projectId);
    SettlementRecord SaveSettlement(SettlementRecord settlement);

    VatAnalysis? VatFor(string projectId);
    VatAnalysis SaveVat(VatAnalysis analysis);

    RetentionRelease? RetentionFor(string projectId);
    RetentionRelease SaveRetention(RetentionRelease release);

    event Action? OnChange;
}
