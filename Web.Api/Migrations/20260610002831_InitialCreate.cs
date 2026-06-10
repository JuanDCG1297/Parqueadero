using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VehicleEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VehicleType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Plate = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    EntryTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EmailSent = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleEntry_Plate_Active",
                table: "VehicleEntries",
                column: "Plate",
                unique: true,
                filter: "[ExitTime] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleEntries");
        }
    }
}
