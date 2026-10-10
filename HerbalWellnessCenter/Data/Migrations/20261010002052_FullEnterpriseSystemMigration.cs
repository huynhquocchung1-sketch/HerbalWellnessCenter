using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HerbalWellnessCenter.Data.Migrations
{
    /// <inheritdoc />
    public partial class FullEnterpriseSystemMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BedId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TherapistId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BedNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RoomName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FloorLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Therapists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Specialty = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Therapists", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_BedId",
                table: "Bookings",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TherapistId",
                table: "Bookings",
                column: "TherapistId");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_IsOccupied",
                table: "Beds",
                column: "IsOccupied");

            migrationBuilder.CreateIndex(
                name: "IX_Therapists_IsAvailable",
                table: "Therapists",
                column: "IsAvailable");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Beds_BedId",
                table: "Bookings",
                column: "BedId",
                principalTable: "Beds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Therapists_TherapistId",
                table: "Bookings",
                column: "TherapistId",
                principalTable: "Therapists",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Beds_BedId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Therapists_TherapistId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropTable(
                name: "Therapists");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_BedId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TherapistId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "BedId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TherapistId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Bookings");
        }
    }
}
