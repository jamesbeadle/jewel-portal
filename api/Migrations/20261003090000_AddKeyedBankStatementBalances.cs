using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The bank statement balances a director keys by hand (2026-10-03, Nigel's ask: Cash in bank
    /// read Xero's balance with 33 unreconciled items in it, not the statement). One table, one
    /// row per Xero bank account, additive only. Script: add-keyed-bank-statement-balances.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20261003090000_AddKeyedBankStatementBalances")]
    public partial class AddKeyedBankStatementBalances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeyedBankStatementBalances",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StatementDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    KeyedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    KeyedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_KeyedBankStatementBalances", x => x.AccountId));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "KeyedBankStatementBalances");
        }
    }
}
