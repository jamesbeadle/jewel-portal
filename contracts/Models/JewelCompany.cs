namespace Jewel.JPMS.Models;

/// <summary>
/// The Jewel company a form, a pack or a register row speaks for. A Jewel Bespoke Build
/// sub-contractor must never sign a Jewel Property Serve document, so the company travels with
/// every link, submission and record. Persisted as an int: append, never reorder.
/// </summary>
public enum JewelCompany
{
    JewelBespokeBuild = 0,
    JewelPropertyServe = 1
}

public sealed record CompanyLink(string Label, string Address);

/// <summary>
/// A company's trading disclosures. A stranger asked for a UTR, an NI number and a passport photo
/// needs a way to check the request is genuine, and a UK company must show its registered
/// particulars; a blank particular is simply not shown. Jewel Property Serve's were confirmed by
/// Jeremy on 16 Aug 2026, with the family line the dashboard's footer closed on; Jewel Bespoke
/// Build's are its Companies House record (read 23 Sep 2026) and the numbers on its purchase orders.
/// </summary>
public sealed record JewelCompanyParticulars(
    JewelCompany Company,
    string Code,
    string LegalName,
    string ShortName,
    string CompanyNumber,
    string RegisteredOffice,
    string VatNumber,
    string Phone,
    string Email,
    string Website,
    IReadOnlyList<CompanyLink> Socials,
    string Family = "");
