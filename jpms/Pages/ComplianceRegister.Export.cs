namespace Jewel.JPMS.Pages;

public partial class ComplianceRegister
{
    private ExcelWorkbook? BuildExportWorkbook(bool ignoreFilters)
    {
        var rows = ignoreFilters ? allRows : FilteredRows;
        if (rows.Count == 0) return null;

        var workbook = new ExcelWorkbook();
        var sheet = workbook.AddSheet("Compliance register",
            new ExcelColumn("Company"),
            new ExcelColumn("Trade"),
            new ExcelColumn("Document"),
            new ExcelColumn("Expires", ExcelFormat.Date),
            new ExcelColumn("PL cover (£)", ExcelFormat.Currency),
            new ExcelColumn("Below £5m PL"),
            new ExcelColumn("Compliance"),
            new ExcelColumn("On site at"));

        foreach (var row in rows)
        {
            sheet.AddRow(
                row.Company.CompanyName,
                row.Company.TradesLabel,
                row.DocumentLabel,
                row.ExpiresAt,
                row.PublicLiabilityCover,
                row.IsBelowPublicLiabilityRequirement ? "Yes" : "",
                row.Status.DisplayName(),
                string.Join(", ", row.OnSiteAt));
        }
        return workbook;
    }
}
