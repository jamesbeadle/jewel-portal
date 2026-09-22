using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The shared site credential a Useful Information note may hold (2026-09-22, Jeremy's ask):
    /// UsefulInformationNotes.SecretCiphertext, AES-GCM under the configured key, revealed to the
    /// directors alone. Nullable, additive — apply before or with the deploy.
    /// Script: add-useful-information-secret.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260922150000_AddUsefulInformationSecret")]
    public partial class AddUsefulInformationSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretCiphertext",
                table: "UsefulInformationNotes",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SecretCiphertext", table: "UsefulInformationNotes");
        }
    }
}
