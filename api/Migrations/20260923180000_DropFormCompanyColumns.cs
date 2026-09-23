using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Step 2 of 2 of taking the second company back out of the onboarding forms (2026-09-23): drops
    /// ComplianceDocuments.FormCompany — first carrying any certificate filed from a form since step 1
    /// onto IsFromAForm — and the Company column on the seven forms tables, with the default step 1
    /// gave it. Run only after the api that no longer reads them is deployed. Scoped script (which
    /// refuses to run before step 1): api/Migrations/drop-form-company-columns.sql. Not reversible —
    /// every form in this portal is Jewel Bespoke Build's, so there is nothing to put back.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260923180000_DropFormCompanyColumns")]
    public partial class DropFormCompanyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF COL_LENGTH(N'ComplianceDocuments', N'FormCompany') IS NOT NULL
BEGIN
    EXEC sp_executesql N'UPDATE [ComplianceDocuments] SET [IsFromAForm] = 1 WHERE [FormCompany] IS NOT NULL AND [IsFromAForm] = 0';
    ALTER TABLE [ComplianceDocuments] DROP COLUMN [FormCompany];
END;");
            foreach (var table in AddComplianceDocumentIsFromAForm.TablesWithACompany) migrationBuilder.Sql(DropCompanyFrom(table));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("The forms' Company columns are not put back — every form in this portal is Jewel Bespoke Build's.");
        }

        private static string DropCompanyFrom(string table) =>
            $@"IF COL_LENGTH(N'{table}', N'Company') IS NOT NULL
BEGIN
    DECLARE @df sysname = (SELECT dc.[name] FROM sys.default_constraints dc
        JOIN sys.columns c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
        WHERE dc.[parent_object_id] = OBJECT_ID(N'[{table}]') AND c.[name] = N'Company');
    IF @df IS NOT NULL EXEC (N'ALTER TABLE [{table}] DROP CONSTRAINT [' + @df + N']');
    ALTER TABLE [{table}] DROP COLUMN [Company];
END;";
    }
}
