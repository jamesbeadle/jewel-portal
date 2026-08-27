namespace Jewel.JPMS.Api.Features.BuildingControl;

internal static class BuildingControlIdentifierFactory
{
    public static string Next() => Guid.NewGuid().ToString("N");
}
