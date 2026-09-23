using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Step 1 of 2 of taking the second company back out of the onboarding forms (2026-09-23): the
    /// forms are Jewel Bespoke Build's alone. ComplianceDocuments gains IsFromAForm — whether a
    /// certificate came in on a form, the only certificates the renewal chase asks for — set from
    /// FormCompany; and the Company column AddOnboardingForms put on seven forms tables gets a default
    /// of 0, so the api that no longer writes it can insert rows until step 2 (DropFormCompanyColumns)
    /// drops it. ADDITIVE: run before the api deploys. Scoped script with the same SQL and a count of
    /// any rows marked for the second company: api/Migrations/add-compliance-document-is-from-a-form.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260923170000_AddComplianceDocumentIsFromAForm")]
    public partial class AddComplianceDocumentIsFromAForm : Migration
    {
        internal static readonly string[] TablesWithACompany =
        {
            "FormPacks", "FormInvites", "FormSubmissions", "FormUploads", "FormFolders", "RightToWorkChecks", "TrainingRecords"
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "IsFromAForm", table: "ComplianceDocuments", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.Sql(@"IF COL_LENGTH(N'ComplianceDocuments', N'FormCompany') IS NOT NULL
    EXEC sp_executesql N'UPDATE [ComplianceDocuments] SET [IsFromAForm] = 1 WHERE [FormCompany] IS NOT NULL AND [IsFromAForm] = 0';");
            foreach (var table in TablesWithACompany) migrationBuilder.Sql(DefaultCompanyOn(table));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in TablesWithACompany)
                migrationBuilder.Sql($"IF OBJECT_ID(N'[DF_{table}_Company]', N'D') IS NOT NULL ALTER TABLE [{table}] DROP CONSTRAINT [DF_{table}_Company];");
            migrationBuilder.DropColumn(name: "IsFromAForm", table: "ComplianceDocuments");
        }

        private static string DefaultCompanyOn(string table) =>
            $@"IF COL_LENGTH(N'{table}', N'Company') IS NOT NULL AND OBJECT_ID(N'[DF_{table}_Company]', N'D') IS NULL
    ALTER TABLE [{table}] ADD CONSTRAINT [DF_{table}_Company] DEFAULT 0 FOR [Company];";
    }
}
