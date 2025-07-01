using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTripStops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Stops",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Stops",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_CompanyId",
                table: "Stops",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_StopName_Latitude_Longitude",
                table: "Stops",
                columns: new[] { "StopName", "Latitude", "Longitude" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Stops_BusCompanies_CompanyId",
                table: "Stops",
                column: "CompanyId",
                principalTable: "BusCompanies",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stops_BusCompanies_CompanyId",
                table: "Stops");

            migrationBuilder.DropIndex(
                name: "IX_Stops_CompanyId",
                table: "Stops");

            migrationBuilder.DropIndex(
                name: "IX_Stops_StopName_Latitude_Longitude",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Stops");
        }
    }
}
