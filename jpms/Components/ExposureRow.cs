namespace Jewel.JPMS.Components;

public sealed record ExposureRow(
    string CompanyName,
    string Trade,
    int WorkOrderCount,
    decimal TotalValue);
