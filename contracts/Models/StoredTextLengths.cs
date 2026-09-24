namespace Jewel.JPMS.Models;

/// <summary>
/// The longest text each kind of short field can hold where it is stored — what a form's
/// <c>maxlength</c> says, so a person is stopped while typing rather than refused on saving.
/// Prose (descriptions, notes, reasons, narratives) has no length limit and no entry here.
/// Every figure is pinned to its column by <c>StoredTextLengthsTests</c>: change a column and
/// the test names the constant that no longer matches.
/// </summary>
public static class StoredTextLengths
{
    public const int Name = 256;
    public const int Title = 256;
    public const int AddressLine = 256;
    public const int Town = 128;
    public const int County = 128;
    public const int Website = 512;
    public const int Reference = 64;
    public const int RegisterReference = 128;
    public const int DocumentReference = 256;
    public const int ProviderName = 128;
    public const int Code = 16;
    public const int Unit = 16;
    public const int SectionName = 128;
    public const int PeriodName = 128;
    public const int GroupName = 128;
    public const int PackageName = 256;
    public const int Trade = 64;
    public const int ContractEdition = 16;
    public const int XeroSiteName = 128;
    public const int EmailSubject = 512;
    public const int EmailRecipients = 2000;
}
