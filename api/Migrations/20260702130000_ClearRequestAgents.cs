using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // DATA ONLY, no schema change. Agents are parked for now: auto-provisioning is disabled
    // (AgentProvisioning.ProvisioningEnabled = false) and the agent workspace is hidden in the UI.
    // This clears every agent row that was auto-provisioned onto requests, along with any agent
    // chat and proposals, so no request carries an agent. Irreversible by design — if agents come
    // back they will be re-provisioned or assigned deliberately, not restored from this data.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260702130000_ClearRequestAgents")]
    public partial class ClearRequestAgents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [AgentProposals];");
            migrationBuilder.Sql("DELETE FROM [AgentChatMessages];");
            migrationBuilder.Sql("DELETE FROM [RequestAgents];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted agent state is not restorable; nothing to do.
        }
    }
}
