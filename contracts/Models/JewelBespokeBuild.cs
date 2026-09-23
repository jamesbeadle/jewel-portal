namespace Jewel.JPMS.Models;

/// <summary>
/// Jewel Bespoke Build's trading disclosures, as the forms show them. A stranger asked for a UTR, an
/// NI number and a passport photo needs a way to check the request is genuine, and a UK company must
/// show its registered particulars. From its Companies House record (read 23 Sep 2026) and the numbers
/// on its purchase orders; the VAT number is not known yet, and a blank particular is not shown.
/// </summary>
public static class JewelBespokeBuild
{
    public const string LegalName = "Jewel Bespoke Build Ltd";
    public const string ShortName = "Jewel Bespoke Build";
    public const string CompanyNumber = "13752749";
    public const string RegisteredOffice = "Argent House, 175 Hook Rise South, Surbiton, England, KT6 7LD";
    public const string VatNumber = "";
    public const string Phone = "0208 109 1014";
    public const string Email = "info@jewelbb.co.uk";
    public const string Website = "https://www.jewelbb.co.uk";
}
