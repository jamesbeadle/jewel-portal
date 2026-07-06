using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Replaces the free-text Subcontractors.PrimaryTrade with a curated Trades table and a
    // SubcontractorTrades link table (a record can carry several trades). Existing values are
    // migrated in place: slash-separated compounds ("Boarding/drylining/Plastering") are split
    // into individual trades, trimmed, first letter capitalised, and de-duplicated
    // case-insensitively before linking.
    [DbContext(typeof(JpmsContext))]
    [Migration("20260706180000_AddCuratedTrades")]
    public partial class AddCuratedTrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    TradeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.TradeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_Name", table: "Trades", column: "Name", unique: true);

            migrationBuilder.CreateTable(
                name: "SubcontractorTrades",
                columns: table => new
                {
                    SubcontractorTradeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TradeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorTrades", x => x.SubcontractorTradeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubcontractorTrades_SubcontractorId_TradeId",
                table: "SubcontractorTrades", columns: new[] { "SubcontractorId", "TradeId" }, unique: true);

            // Seed the curated list from the existing free-text values: split on "/", trim,
            // capitalise the first letter. DISTINCT under the database's case-insensitive
            // collation folds duplicates like "Plastering" vs "plastering".
            migrationBuilder.Sql(@"
;WITH split AS (
    SELECT LTRIM(RTRIM(part.value)) AS RawTrade
    FROM dbo.Subcontractors s
    CROSS APPLY STRING_SPLIT(s.PrimaryTrade, '/') AS part
    WHERE LTRIM(RTRIM(part.value)) <> N''
),
canon AS (
    SELECT DISTINCT UPPER(LEFT(RawTrade, 1)) + SUBSTRING(RawTrade, 2, LEN(RawTrade)) AS TradeName
    FROM split
)
INSERT INTO dbo.Trades (TradeId, Name, CreatedAt)
SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), TradeName, SYSDATETIMEOFFSET()
FROM canon;
");

            // Link every record to each of its (split) trades.
            migrationBuilder.Sql(@"
;WITH split AS (
    SELECT s.SubcontractorId, LTRIM(RTRIM(part.value)) AS RawTrade
    FROM dbo.Subcontractors s
    CROSS APPLY STRING_SPLIT(s.PrimaryTrade, '/') AS part
    WHERE LTRIM(RTRIM(part.value)) <> N''
),
canon AS (
    SELECT DISTINCT SubcontractorId,
           UPPER(LEFT(RawTrade, 1)) + SUBSTRING(RawTrade, 2, LEN(RawTrade)) AS TradeName
    FROM split
)
INSERT INTO dbo.SubcontractorTrades (SubcontractorTradeId, SubcontractorId, TradeId)
SELECT LOWER(REPLACE(CONVERT(nvarchar(36), NEWID()), '-', '')), canon.SubcontractorId, t.TradeId
FROM canon
JOIN dbo.Trades t ON t.Name = canon.TradeName;
");

            migrationBuilder.DropColumn(name: "PrimaryTrade", table: "Subcontractors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryTrade", table: "Subcontractors", type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "");

            // Best-effort reconstruction of the old free-text value (joined with "/"). Truncated to
            // the column's 64 characters if a record carries an unusually long trade list.
            migrationBuilder.Sql(@"
UPDATE s SET s.PrimaryTrade = LEFT(joined.Names, 64)
FROM dbo.Subcontractors s
CROSS APPLY (
    SELECT STRING_AGG(t.Name, '/') WITHIN GROUP (ORDER BY t.Name) AS Names
    FROM dbo.SubcontractorTrades st
    JOIN dbo.Trades t ON t.TradeId = st.TradeId
    WHERE st.SubcontractorId = s.SubcontractorId
) AS joined
WHERE joined.Names IS NOT NULL;
");

            migrationBuilder.DropTable(name: "SubcontractorTrades");
            migrationBuilder.DropTable(name: "Trades");
        }

        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            // Runtime applies Up()/Down() directly; future scaffolding uses JpmsContextModelSnapshot.
        }
    }
}
