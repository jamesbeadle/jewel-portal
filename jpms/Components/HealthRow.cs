namespace Jewel.JPMS.Components;

public sealed record HealthRow(
    string Reference,
    string Name,
    int OverdueRfis,
    int OpenVariations,
    int UnissuedValuations);
