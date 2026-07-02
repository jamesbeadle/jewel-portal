using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Entity refactor: architects become a first-class entity, managed separately from clients.
    //
    // Jewel either works directly with a client (no architect) or through an architect acting on
    // the client's behalf — previously the architect was two free-text fields on the client
    // account, which is the wrong shape for the domain.
    //
    // 1. New Architects table: a global architect practice (name + contact), parallel to Clients.
    // 2. Existing client architect details are migrated into Architects (de-duplicated by contact
    //    email, then by name for email-less rows).
    // 3. Requests and Projects generalise their client link into a party link:
    //    ClientId -> PartyId, plus PartyKind (0 = Client, 1 = Architect) and OnBehalfOfClientId
    //    (the client an architect acts for — optional, only meaningful when PartyKind = 1).
    //    Rows whose client carried architect details are re-pointed at the migrated architect with
    //    the old client kept as OnBehalfOfClientId, preserving today's behaviour (RFIs went to the
    //    client's architect).
    // 4. Clients drops ArchitectName / ArchitectEmail / RequestEmailPreference. Recipient
    //    resolution now follows the party: architect party -> architect contact email, client
    //    party -> client primary contact email.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260702170000_SeparateArchitectsFromClients")]
    public partial class SeparateArchitectsFromClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Architects",
                columns: table => new
                {
                    ArchitectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Architects", x => x.ArchitectId);
                });

            // Seed architects from the architect details previously stored on client accounts.
            // One architect per distinct contact email; name-only rows are grouped by name.
            migrationBuilder.Sql(@"
INSERT INTO Architects (ArchitectId, Name, ContactName, ContactEmail, CreatedAt)
SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
       COALESCE(MAX(ArchitectName), ArchitectEmail),
       MAX(ArchitectName),
       ArchitectEmail,
       SYSDATETIMEOFFSET()
FROM Clients
WHERE ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(ArchitectEmail)) <> ''
GROUP BY ArchitectEmail;

INSERT INTO Architects (ArchitectId, Name, ContactName, ContactEmail, CreatedAt)
SELECT LOWER(REPLACE(CONVERT(nvarchar(64), NEWID()), '-', '')),
       ArchitectName,
       ArchitectName,
       NULL,
       SYSDATETIMEOFFSET()
FROM Clients
WHERE (ArchitectEmail IS NULL OR LTRIM(RTRIM(ArchitectEmail)) = '')
  AND ArchitectName IS NOT NULL AND LTRIM(RTRIM(ArchitectName)) <> ''
GROUP BY ArchitectName;
");

            // Requests: ClientId becomes the party link.
            migrationBuilder.RenameColumn(name: "ClientId", table: "Requests", newName: "PartyId");
            migrationBuilder.AddColumn<int>(
                name: "PartyKind", table: "Requests", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(
                name: "OnBehalfOfClientId", table: "Requests", type: "nvarchar(64)", maxLength: 64, nullable: true);

            // Projects: same generalisation.
            migrationBuilder.RenameColumn(name: "ClientId", table: "Projects", newName: "PartyId");
            migrationBuilder.AddColumn<int>(
                name: "PartyKind", table: "Projects", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<string>(
                name: "OnBehalfOfClientId", table: "Projects", type: "nvarchar(64)", maxLength: 64, nullable: true);

            // Re-point rows whose linked client carried an architect: the architect becomes the
            // party (kind 1) and the old client is kept as the on-behalf-of client. This preserves
            // today's behaviour — RFIs were issued to the client's architect.
            foreach (var table in new[] { "Requests", "Projects" })
            {
                migrationBuilder.Sql($@"
UPDATE t
SET t.PartyKind = 1,
    t.OnBehalfOfClientId = t.PartyId,
    t.PartyId = a.ArchitectId
FROM {table} t
JOIN Clients c ON c.ClientId = t.PartyId
JOIN Architects a
  ON (c.ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(c.ArchitectEmail)) <> '' AND a.ContactEmail = c.ArchitectEmail)
  OR ((c.ArchitectEmail IS NULL OR LTRIM(RTRIM(c.ArchitectEmail)) = '') AND a.ContactEmail IS NULL AND a.Name = c.ArchitectName)
WHERE (c.ArchitectEmail IS NOT NULL AND LTRIM(RTRIM(c.ArchitectEmail)) <> '')
   OR (c.ArchitectName IS NOT NULL AND LTRIM(RTRIM(c.ArchitectName)) <> '');
");
            }

            migrationBuilder.DropColumn(name: "ArchitectName", table: "Clients");
            migrationBuilder.DropColumn(name: "ArchitectEmail", table: "Clients");
            migrationBuilder.DropColumn(name: "RequestEmailPreference", table: "Clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ArchitectName", table: "Clients", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "ArchitectEmail", table: "Clients", type: "nvarchar(256)", maxLength: 256, nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "RequestEmailPreference", table: "Clients", type: "int", nullable: false, defaultValue: 0);

            // Best-effort restore of architect details onto clients that acted through one.
            migrationBuilder.Sql(@"
UPDATE c
SET c.ArchitectName = a.ContactName, c.ArchitectEmail = a.ContactEmail
FROM Clients c
JOIN Projects p ON p.OnBehalfOfClientId = c.ClientId AND p.PartyKind = 1
JOIN Architects a ON a.ArchitectId = p.PartyId;

UPDATE c
SET c.ArchitectName = a.ContactName, c.ArchitectEmail = a.ContactEmail
FROM Clients c
JOIN Requests r ON r.OnBehalfOfClientId = c.ClientId AND r.PartyKind = 1
JOIN Architects a ON a.ArchitectId = r.PartyId;
");

            // Collapse architect parties back to their on-behalf-of client before the columns go.
            foreach (var table in new[] { "Requests", "Projects" })
            {
                migrationBuilder.Sql($@"
UPDATE {table} SET PartyId = OnBehalfOfClientId WHERE PartyKind = 1;
");
            }

            migrationBuilder.DropColumn(name: "PartyKind", table: "Requests");
            migrationBuilder.DropColumn(name: "OnBehalfOfClientId", table: "Requests");
            migrationBuilder.RenameColumn(name: "PartyId", table: "Requests", newName: "ClientId");

            migrationBuilder.DropColumn(name: "PartyKind", table: "Projects");
            migrationBuilder.DropColumn(name: "OnBehalfOfClientId", table: "Projects");
            migrationBuilder.RenameColumn(name: "PartyId", table: "Projects", newName: "ClientId");

            migrationBuilder.DropTable(name: "Architects");
        }
    }
}
