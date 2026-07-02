namespace Jewel.JPMS.Api.Features.ValuationInvoices;

internal static class ValuationInvoicesIdentifierFactory
{
    private const string CompactGuidFormat = "N";
    public const string Prefix = "VI-";

    public static string NextValuationInvoiceId() => Guid.NewGuid().ToString(CompactGuidFormat);

    /// <summary>Human reference for a valuation-invoice number, e.g. 1 => "VI-0001".</summary>
    public static string Reference(int number) => $"{Prefix}{number:0000}";
}
