using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds the AI connector's OAuth storage (docs/ai/10-mcp-connector.md): dynamically registered
    /// client software, short-lived single-use authorisation codes, and the per-user bearer tokens
    /// the MCP endpoint accepts. Purely additive — apply before or with the deploy. Secrets are
    /// stored as SHA-256 hashes only, the same rule as UserSessions.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260827190000_AddAiConnectorOAuth")]
    public partial class AddAiConnectorOAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OAuthClients",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RedirectUrisJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_OAuthClients", x => x.ClientId));

            migrationBuilder.CreateTable(
                name: "OAuthAuthCodes",
                columns: table => new
                {
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RedirectUri = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    CodeChallenge = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OAuthAuthCodes", x => x.CodeHash));

            migrationBuilder.CreateTable(
                name: "OAuthTokens",
                columns: table => new
                {
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FamilyId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OAuthTokens", x => x.TokenHash));

            migrationBuilder.CreateIndex(name: "IX_OAuthTokens_UserEmail_Kind", table: "OAuthTokens", columns: new[] { "UserEmail", "Kind" });
            migrationBuilder.CreateIndex(name: "IX_OAuthTokens_FamilyId", table: "OAuthTokens", column: "FamilyId");
            migrationBuilder.CreateIndex(name: "IX_OAuthAuthCodes_ExpiresAt", table: "OAuthAuthCodes", column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OAuthTokens");
            migrationBuilder.DropTable(name: "OAuthAuthCodes");
            migrationBuilder.DropTable(name: "OAuthClients");
        }
    }
}
