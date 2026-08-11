using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Undirected to-do ↔ to-do links (TodoItemLinkEntity): one row per pair, the two ids stored
    // in canonical order (TodoItemAId < TodoItemBId, ordinal — TodoItemLinkPairs.Normalise) so
    // A→B and B→A cannot exist as two rows. House style: loose string ids, no FK constraints;
    // DeleteTodoItemHandler sweeps rows naming a deleted item. Purely additive — deploy order
    // with the code doesn't matter, but run it first anyway.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260811100000_AddTodoItemLinks")]
    public partial class AddTodoItemLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TodoItemLinks",
                columns: table => new
                {
                    TodoItemLinkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TodoItemAId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TodoItemBId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LinkedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LinkedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_TodoItemLinks", x => x.TodoItemLinkId));

            // Unique on the canonically-ordered pair: the same two items can only be linked once,
            // and "everything linked to X" seeks this index for the A side…
            migrationBuilder.CreateIndex(
                name: "IX_TodoItemLinks_TodoItemAId_TodoItemBId",
                table: "TodoItemLinks",
                columns: new[] { "TodoItemAId", "TodoItemBId" },
                unique: true);

            // …and this one for the B side.
            migrationBuilder.CreateIndex(
                name: "IX_TodoItemLinks_TodoItemBId",
                table: "TodoItemLinks",
                column: "TodoItemBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TodoItemLinks");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
