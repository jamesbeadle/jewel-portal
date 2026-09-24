namespace Jewel.JPMS.Contracts.Progress;

public static class ContractorsReportBuildingControlReading
{
    /// <summary>Whether Section 7's contact comes from the project's Building Control case; when it
    /// does not, the report's entered contact line prints instead.</summary>
    public static bool HasCase(this ContractorsReportBuildingControl buildingControl) =>
        buildingControl.BodyName is not null || buildingControl.ContactName is not null;
}
