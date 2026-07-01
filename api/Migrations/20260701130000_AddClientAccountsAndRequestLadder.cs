using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Entity refactor — Phase 1: Request ladder + Client accounts.
    //  - New Clients table: a global client account holding the architect email that RFIs are issued
    //    to when a request is promoted. (One client can own many projects.)
    //  - Requests gains HasRfq (an RFI that has spawned an RFQ, unlocking VOQ creation) and ClientId
    //    (the owning client account; architect-email source on RFI promotion). Both are additive and
    //    nullable/defaulted, so existing rows are unaffected (default General/HasRfq=false is applied
    //    at the model level; existing rows keep their current Kind).
    [DbContext(typeof(JpmsContext))]
    [Migration("20260701130000_AddClientAccountsAndRequestLadder")]
    public partial class AddClientAccountsAndRequestLadder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PrimaryContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ArchitectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ArchitectEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                });

            migrationBuilder.AddColumn<bool>(
                name: "HasRfq", table: "Requests", type: "bit", nullable: false, defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClientId", table: "Requests", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "HasRfq", table: "Requests");
            migrationBuilder.DropColumn(name: "ClientId", table: "Requests");
            migrationBuilder.DropTable(name: "Clients");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
