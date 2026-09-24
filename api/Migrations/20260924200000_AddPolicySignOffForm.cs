using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The Policy sign-off form (2026-09-24, the FD's task from Jeremy): a published policy revision
    /// carries its declaration and the PDF people read; a form link carries the revision it was sent
    /// for; and a sign-off made through the form records the person's name, company, position, the
    /// link and the form. Additive only. Script: add-policy-sign-off-form.sql. Apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260924200000_AddPolicySignOffForm")]
    public partial class AddPolicySignOffForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Declaration", table: "PolicyDocuments", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "FileName", table: "PolicyDocuments", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "FileBlobRef", table: "PolicyDocuments", type: "nvarchar(512)", maxLength: 512, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "RecipientName", table: "PolicySignOffs", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "CompanyName", table: "PolicySignOffs", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Position", table: "PolicySignOffs", type: "nvarchar(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "FormInviteId", table: "PolicySignOffs", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "FormSubmissionId", table: "PolicySignOffs", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<string>(name: "PolicyDocumentId", table: "FormInvites", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Declaration", table: "PolicyDocuments");
            migrationBuilder.DropColumn(name: "FileName", table: "PolicyDocuments");
            migrationBuilder.DropColumn(name: "FileBlobRef", table: "PolicyDocuments");
            migrationBuilder.DropColumn(name: "RecipientName", table: "PolicySignOffs");
            migrationBuilder.DropColumn(name: "CompanyName", table: "PolicySignOffs");
            migrationBuilder.DropColumn(name: "Position", table: "PolicySignOffs");
            migrationBuilder.DropColumn(name: "FormInviteId", table: "PolicySignOffs");
            migrationBuilder.DropColumn(name: "FormSubmissionId", table: "PolicySignOffs");
            migrationBuilder.DropColumn(name: "PolicyDocumentId", table: "FormInvites");
        }
    }
}
