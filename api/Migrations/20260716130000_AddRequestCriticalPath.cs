using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Requests gain the Critical Path tag: a flag marking an RFI as programme-related — its
    // answer gates work on the programme's critical path. Surfaces the RFI in the project
    // Programme tab's "Critical Path RFIs" view. Defaults to off for every existing row;
    // user-set from the RFI detail page thereafter.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260716130000_AddRequestCriticalPath")]
    public partial class AddRequestCriticalPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CriticalPath",
                table: "Requests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CriticalPath",
                table: "Requests");
        }
    }
}
