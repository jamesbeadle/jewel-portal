namespace Jewel.JPMS.Api.Features.Todos;

internal static class TodosIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string Next() => Guid.NewGuid().ToString(CompactGuidFormat);
}
