using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusTicketSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TripType",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Notifications",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecipientCompanyId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTwoStory",
                table: "Buses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Buses",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientCompanyId",
                table: "Notifications",
                column: "RecipientCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_BusCompanies_RecipientCompanyId",
                table: "Notifications",
                column: "RecipientCompanyId",
                principalTable: "BusCompanies",
                principalColumn: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_BusCompanies_RecipientCompanyId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientCompanyId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TripType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RecipientCompanyId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IsTwoStory",
                table: "Buses");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Buses");
        }
    }
}
