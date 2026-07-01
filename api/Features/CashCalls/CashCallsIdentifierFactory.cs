namespace Jewel.JPMS.Api.Features.CashCalls;

internal static class CashCallsIdentifierFactory
{
    private const string CompactGuidFormat = "N";
    public const string Prefix = "CC-";

    public static string NextCashCallId() => Guid.NewGuid().ToString(CompactGuidFormat);

    /// <summary>Human reference for a cash-call number, e.g. 1 => "CC-0001".</summary>
    public static string Reference(int number) => $"{Prefix}{number:0000}";
}
