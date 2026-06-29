using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestAgents",
                columns: table => new
                {
                    RequestAgentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AssignedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestAgents", x => x.RequestAgentId);
                });

            migrationBuilder.CreateTable(
                name: "AgentChatMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AuthorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentChatMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "AgentProposals",
                columns: table => new
                {
                    ProposalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AgentKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Rationale = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StructuredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentProposals", x => x.ProposalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestAgents_RequestId",
                table: "RequestAgents",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentChatMessages_RequestId_AgentKey",
                table: "AgentChatMessages",
                columns: new[] { "RequestId", "AgentKey" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentProposals_RequestId",
                table: "AgentProposals",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestAgents");

            migrationBuilder.DropTable(
                name: "AgentChatMessages");

            migrationBuilder.DropTable(
                name: "AgentProposals");
        }
    }
}
