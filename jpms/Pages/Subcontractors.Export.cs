using static Jewel.JPMS.Features.Directory.DirectoryDisplay;

namespace Jewel.JPMS.Pages;

public partial class Subcontractors
{
    // The screen's "Primary contact" cell is compound (name + email subtitle) — split
    // into two columns so the email isn't lost from the export.
    private ExcelWorkbook? BuildExportWorkbook(bool ignoreFilters)
    {
        // "Ignore search & filter" (offered while either is narrowing the table) exports the
        // complete directory in the same company-name order.
        var subs = ignoreFilters
            ? DirectoryCompanies().OrderBy(s => s.CompanyName).ToList()
            : Filtered();
        if (subs.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Directory",
            new ExcelColumn("Company"),
            new ExcelColumn("Type"),
            new ExcelColumn("Trade"),
            new ExcelColumn("Primary contact"),
            new ExcelColumn("Contact email"),
            new ExcelColumn("Location"));

        foreach (var sub in subs)
        {
            sheet.AddRow(
                sub.CompanyName,
                Label(sub.Category),
                sub.TradesLabel,
                sub.ContactName,
                sub.ContactEmail,
                Location(sub));
        }
        return workbook;
    }
}
