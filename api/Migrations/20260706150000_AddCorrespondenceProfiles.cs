using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Correspondence profiles: the single "correspondence with" address becomes a full To/CC/BCC
    // recipient set (see docs/Correspondence-Profile-To-CC-BCC-Plan.md).
    //
    // 1. New PartyContacts table: the people at a client account or architect practice, each with
    //    a default routing (None 0 / To 1 / Cc 2 / Bcc 3). Seeded from the legacy single-contact
    //    fields — Clients.PrimaryContactName/Email and Architects.ContactName/Email — as the
    //    party's primary To correspondent, so resolution behaviour is unchanged on upgrade.
    // 2. ProjectContacts gains Routing (same enum; backfilled from the ReceivesRequests flag:
    //    true -> To, false -> None) and an optional PartyContactId link that turns a row into a
    //    per-project routing override for a person on the party's contact book.
    //    ReceivesRequests survives for one release (kept equal to Routing == To on write).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260706150000_AddCorrespondenceProfiles")]
    public partial class AddCorrespondenceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartyContacts",
                columns: table => new
                {
                    PartyContactId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PartyKind = table.Column<int>(type: "int", nullable: false),
                    PartyId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DefaultRouting = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.PartyContactId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyKind_PartyId",
                table: "PartyContacts",
                columns: new[] { "PartyKind", "PartyId" });

            // Seed each party's legacy single contact as its primary To correspondent
            // (PartyKind: 0 = Client, 1 = Architect; DefaultRouting: 1 = To).
            migrationBuilder.Sql(@"
INSERT INTO PartyContacts (PartyContactId, PartyKind, PartyId, Name, Email, JobTitle, DefaultRouting, IsPrimary, CreatedAt)
SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
       0,
       ClientId,
       COALESCE(NULLIF(LTRIM(RTRIM(PrimaryContactName)), ''), PrimaryContactEmail),
       LTRIM(RTRIM(PrimaryContactEmail)),
       NULL,
       1,
       1,
       SYSDATETIMEOFFSET()
FROM Clients
WHERE PrimaryContactEmail IS NOT NULL AND LTRIM(RTRIM(PrimaryContactEmail)) <> '';

INSERT INTO PartyContacts (PartyContactId, PartyKind, PartyId, Name, Email, JobTitle, DefaultRouting, IsPrimary, CreatedAt)
SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
       1,
       ArchitectId,
       COALESCE(NULLIF(LTRIM(RTRIM(ContactName)), ''), ContactEmail),
       LTRIM(RTRIM(ContactEmail)),
       NULL,
       1,
       1,
       SYSDATETIMEOFFSET()
FROM Architects
WHERE ContactEmail IS NOT NULL AND LTRIM(RTRIM(ContactEmail)) <> '';
");

            // Project profile: routing per row (backfilled from the legacy flag) plus the optional
            // party-contact link.
            migrationBuilder.AddColumn<int>(
                name: "Routing", table: "ProjectContacts", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(
                name: "PartyContactId", table: "ProjectContacts", type: "nvarchar(64)", maxLength: 64, nullable: true);

            migrationBuilder.Sql("UPDATE ProjectContacts SET Routing = 1 WHERE ReceivesRequests = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ProjectContacts SET ReceivesRequests = 1 WHERE Routing = 1;");
            migrationBuilder.DropColumn(name: "PartyContactId", table: "ProjectContacts");
            migrationBuilder.DropColumn(name: "Routing", table: "ProjectContacts");
            migrationBuilder.DropTable(name: "PartyContacts");
        }
    }
}
