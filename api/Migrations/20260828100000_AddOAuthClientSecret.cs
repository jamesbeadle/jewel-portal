using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Adds OAuthClients.SecretHash (2026-08-28): dynamic client registration now issues a client
    /// secret alongside the client id, because Perplexity's connector refuses a registration
    /// response without one — even while registering as a public client. Only the SHA-256 hash is
    /// stored; PKCE remains the flow's real protection and the secret is verified only when
    /// presented. Nullable, so clients registered before this column (Claude's among them) keep
    /// working untouched. Additive — apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260828100000_AddOAuthClientSecret")]
    public partial class AddOAuthClientSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretHash",
                table: "OAuthClients",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretHash",
                table: "OAuthClients");
        }
    }
}
