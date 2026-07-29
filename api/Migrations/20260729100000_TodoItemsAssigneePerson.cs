using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // A to-do item can now optionally be PINNED to one named holder of its assigned role:
    // AssigneePersonEmail (a DirectoryUsers email) narrows the item from "everyone holding the
    // role" to that one person's list. The role stays the primary assignment — a pin is never set
    // without it, and the directory commands clear the pin when the person is removed or loses the
    // role, so the item falls back to the role rather than orphaning (the failure the 20260716
    // TodoItemsAssignToRole migration removed person-assignment to avoid). Null = unpinned, which
    // every existing row is.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260729100000_TodoItemsAssigneePerson")]
    public partial class TodoItemsAssigneePerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssigneePersonEmail", table: "TodoItems", type: "nvarchar(256)",
                maxLength: 256, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "AssigneePersonEmail", table: "TodoItems");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
