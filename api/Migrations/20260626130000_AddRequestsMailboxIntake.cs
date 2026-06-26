using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestsMailboxIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "RequestMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EmailMessageId",
                table: "RequestMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InReplyTo",
                table: "RequestMessages",
                type: "nvarchar(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversationId",
                table: "RequestMessages",
                type: "nvarchar(998)",
                maxLength: 998,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SentStatus",
                table: "RequestMessages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IntakeEmails",
                columns: table => new
                {
                    IntakeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GraphMessageId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ConversationId = table.Column<string>(type: "nvarchar(998)", maxLength: 998, nullable: true),
                    InReplyTo = table.Column<string>(type: "nvarchar(998)", maxLength: 998, nullable: true),
                    ReferencesHeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BodyPreview = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    HasAttachments = table.Column<bool>(type: "bit", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaimedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LinkedRequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeEmails", x => x.IntakeId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntakeEmails");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "RequestMessages");

            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "RequestMessages");

            migrationBuilder.DropColumn(
                name: "InReplyTo",
                table: "RequestMessages");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "RequestMessages");

            migrationBuilder.DropColumn(
                name: "SentStatus",
                table: "RequestMessages");
        }
    }
}
