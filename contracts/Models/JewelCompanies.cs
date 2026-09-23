namespace Jewel.JPMS.Models;

/// <summary>The two Jewel companies' particulars, and finding one by its code in a form's address.</summary>
public static class JewelCompanies
{
    public static readonly JewelCompanyParticulars BespokeBuild = new(
        JewelCompany.JewelBespokeBuild, "jbb", "Jewel Bespoke Build Ltd", "Jewel Bespoke Build", "13752749",
        "Argent House, 175 Hook Rise South, Surbiton, England, KT6 7LD", "", "0208 109 1014",
        "info@jewelbb.co.uk", "https://www.jewelbb.co.uk", Array.Empty<CompanyLink>());

    public static readonly JewelCompanyParticulars PropertyServe = new(
        JewelCompany.JewelPropertyServe, "jps", "Jewel Property Serve Ltd", "Jewel Property Serve", "10587343",
        "48 Warwick Street, London, W1B 5AW", "423 8095 95", "0208 109 1012",
        "enquiries@jewelps.co.uk", "https://www.jewelps.co.uk", new[]
        {
            new CompanyLink("LinkedIn", "https://www.linkedin.com/company/jewelpropertyserveltd/"),
            new CompanyLink("Instagram", "https://www.instagram.com/jewelpsltd/"),
            new CompanyLink("Facebook", "https://www.facebook.com/Jewelpsltd/")
        },
        "Part of the Jewel Enterprises Family");

    public static IReadOnlyList<JewelCompanyParticulars> All { get; } = new[] { BespokeBuild, PropertyServe };

    public static JewelCompanyParticulars For(JewelCompany company) =>
        company == JewelCompany.JewelPropertyServe ? PropertyServe : BespokeBuild;

    public static JewelCompanyParticulars? ForCode(string? code) =>
        All.FirstOrDefault(particulars => string.Equals(particulars.Code, code, StringComparison.OrdinalIgnoreCase));
}
