using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    // Weather conditions on progress updates, entered manually by the site manager: a summary
    // ("Partly cloudy with showers"), the observation time, high/low temperature (°C), wind (mph),
    // humidity (%) and total precipitation (inches) — matching the daily-log report layout the
    // site team already issues. All optional: a blank summary with all-null figures means
    // "no weather recorded".
    [DbContext(typeof(JpmsContext))]
    [Migration("20260716100000_AddProgressUpdateWeather")]
    public partial class AddProgressUpdateWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeatherSummary",
                table: "ProgressUpdates",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WeatherObservedAt",
                table: "ProgressUpdates",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeatherTempHighC",
                table: "ProgressUpdates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeatherTempLowC",
                table: "ProgressUpdates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeatherWindMph",
                table: "ProgressUpdates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeatherHumidityPercent",
                table: "ProgressUpdates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeatherPrecipInches",
                table: "ProgressUpdates",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "WeatherSummary", table: "ProgressUpdates");
            migrationBuilder.DropColumn(name: "WeatherObservedAt", table: "ProgressUpdates");
            migrationBuilder.DropColumn(name: "WeatherTempHighC", table: "ProgressUpdates");
            migrationBuilder.DropColumn(name: "WeatherTempLowC", table: "ProgressUpdates");
            migrationBuilder.DropColumn(name: "WeatherWindMph", table: "ProgressUpdates");
            migrationBuilder.DropColumn(name: "WeatherHumidityPercent", table: "ProgressUpdates");
            migrationBuilder.DropColumn(name: "WeatherPrecipInches", table: "ProgressUpdates");
        }
    }
}
