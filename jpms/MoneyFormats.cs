namespace Jewel.JPMS;

/// <summary>
/// The one way money reads across the portal: GB pounds, pence shown ("£1,234.56") or whole
/// pounds ("£1,235") where a summary wants round figures.
/// </summary>
public static class MoneyFormats
{
    private static readonly System.Globalization.CultureInfo BritishEnglish =
        System.Globalization.CultureInfo.GetCultureInfo("en-GB");

    public static string Money(decimal value) => value.ToString("C2", BritishEnglish);

    public static string WholeMoney(decimal value) => value.ToString("C0", BritishEnglish);
}
