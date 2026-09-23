namespace Jewel.JPMS.Api.Features.Forms;

/// <summary>Every forms row's key: a Guid in "N" form, as the portal's other string keys are.</summary>
internal static class FormIdentifierFactory
{
    private const string CompactGuidFormat = "N";

    public static string NextId() => Guid.NewGuid().ToString(CompactGuidFormat);
}
